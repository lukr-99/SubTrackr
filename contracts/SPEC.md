# SubTrackr shared specification

This is the human-readable source of truth for both apps:

- Desktop: WPF, C#, .NET 10 (`desktop/`)
- Android: Kotlin, Jetpack Compose (`android/`)

The apps share no code. Three contracts keep them in step:

1. Data shape: [`proto/subtrackr.proto`](proto/subtrackr.proto). C# generates its models from it;
   Kotlin mirrors it as `@Serializable` classes with the same JSON names.
2. Behavior: the golden vectors in [`vectors/`](vectors/). Both test suites load the same files.
3. This document, which the code and the vectors both trace back to.

Rule: when behavior changes, the spec and the vectors change first, then both apps.

## 1. Domain model

See `subtrackr.proto` for the fields. Key decisions:

- Money is integer `minor_units` plus `exponent`, never a float. `5.12 EUR` is
  `{currency: "EUR", minor_units: 512, exponent: 2}`.
- IDs are UUID v4, generated once and stable across devices. Sync depends on this.
- Soft delete: a non-empty `deleted_at` makes the record a tombstone. The UI hides it; sync keeps it
  so the deletion reaches other devices.
- `updated_at` changes on every edit and drives last-writer-wins.

## 2. Monthly-equivalent normalization

Every subscription reduces to a monthly-equivalent amount in its own currency, computed in exact
decimal. `AVG_DAYS_PER_MONTH = 30.436875` (365.2425 / 12).

| Billing cycle | Monthly equivalent |
| --- | --- |
| `WEEKLY` | `cost * 52 / 12` |
| `MONTHLY` | `cost` |
| `QUARTERLY` | `cost / 3` |
| `SEMIANNUAL` | `cost / 6` |
| `ANNUAL` | `cost / 12` |
| `CUSTOM_DAYS(n)` | `cost * AVG_DAYS_PER_MONTH / n` |

Yearly equivalent is `monthly_equivalent * 12`. `PAUSED` subscriptions stay listed but do not count
toward active spend.

## 3. Currency conversion

- Settings hold a base currency and totals roll up into it:
  `amount_base = amount_source * rate(source -> base)`.
- Rates come from the Frankfurter API (ECB data). Offline, the apps use a built-in fallback table.
  The UI shows where the rates came from and their date.
- Keep full precision through every step. Round only the displayed figure, half-up, to the
  currency's exponent. Sum first, then round.

## 4. Totals

- Monthly active spend (base) is the sum over `ACTIVE`, non-deleted subscriptions of
  `convert(monthly_equivalent, base)`, rounded at the end.
- Yearly active spend is monthly times 12.
- Per-currency subtotals (before conversion) are shown as well.

## 5. Worth-it helper

Each subscription has a `worth_mode`:

- `ESSENTIAL`: verdict ESSENTIAL, never flagged as not worth it.
- `WORTH`: verdict WORTH.
- `NOT_WORTH`: verdict NOT_WORTH.
- `AUTO` (default): computed from usage.
  - `cost_per_use = monthly_equivalent_base / uses_per_month`, displayed to 4 decimal places.
  - `cost_per_use <= T` gives WORTH, `> T` gives NOT_WORTH, and `uses_per_month == 0` gives UNKNOWN.

`cost_per_use` is always computed and shown. `T` is `Settings.worth_threshold`, stored per device;
`0` selects a currency-aware default (about 1.50 for EUR, USD, GBP; 35 for CZK; 500 for HUF; 200 for
JPY).

## 6. What-if calculator

An in-memory scenario: clone the active subscriptions, add, remove, or edit hypothetical entries,
and recompute section 4 totals plus the difference from the real total. It uses the same
normalization and conversion code.

## 7. Versioning

- `schema_version` in `Database` and `Settings` gates local data migrations.
- Both apps share one SemVer version, `SubTrackrVersion` in the root `Version.props`.
- Release builds carry `X.Y.Z`. Every other build carries `X.Y.Z-dev`, stores its data apart from
  the released app (desktop `%APPDATA%\SubTrackr Dev`, Android application ID suffix `.dev`), and
  never checks for updates.
- Android `versionCode` is `major * 10000 + minor * 100 + patch`.

## 8. Sync

Sync is optional, last-writer-wins per subscription, and runs against a Supabase project the user
owns. Only `subscriptions` sync. `Settings` stay on the device.

### 8.1 Merge

`Merge(local, remote) -> merged`, verified by `vectors/merge.json`:

- Records match by `id`. `updated_at` is ISO-8601 UTC (`2026-08-28T10:00:00Z`), so string
  comparison equals time order.
- For every `id` on either side, keep the record with the greater `updated_at`.
- On a tie, a tombstone beats a live record. If still tied, remote wins.
- Tombstones stay in the merged result.
- A sync pass is pull, merge, save locally, push the merged set.

### 8.2 Authentication

Every sync request runs as a signed-in Supabase Auth user. The publishable key only identifies the
project. Sign-in uses a 6-digit email code through the Auth REST API, with the headers
`apikey: <publishable key>` and `Content-Type: application/json`:

| Step | Request | Body |
| --- | --- | --- |
| Send code | `POST {url}/auth/v1/otp` | `{"email": e, "create_user": true}` |
| Verify | `POST {url}/auth/v1/verify` | `{"type": "email", "email": e, "token": code}` |
| Refresh | `POST {url}/auth/v1/token?grant_type=refresh_token` | `{"refresh_token": r}` |
| Sign out | `POST {url}/auth/v1/logout` with `Authorization: Bearer <access>` | none |

Verify and refresh return `access_token`, `refresh_token`, `expires_in` (seconds), and
`user.id`. The apps:

- store the session only in an OS-protected store (Windows DPAPI for the current user; an AES-GCM
  key in the Android Keystore), never in `data.json`, backups, or logs;
- refresh when the access token expires within 60 seconds, and once after an HTTP 401 before
  giving up;
- sign out and drop the session when the sync URL or key changes, or a refresh is rejected;
- treat sign-out as local first: the stored session is deleted even if the logout call fails.

### 8.3 Rows

Rows live in the `subscriptions` table created by `supabase/migrations/`. The primary key is
`(user_id, id)`, and row-level security limits every operation to `user_id = auth.uid()`.
`vectors/sync-rows.json` pins the mapping between a `Subscription` and a row:

- Enums are their proto names as text (`MONTHLY`, `PAUSED`, `NOT_WORTH`).
- Money is three columns: `cost_currency`, `cost_minor`, `cost_exponent`.
- Every pushed row carries `user_id`, the signed-in user's ID.
- A missing or unknown enum value reads as `MONTHLY`, `ACTIVE`, or `AUTO`. Any other missing column
  reads as an empty string, zero, or false.

Pull is `GET {url}/rest/v1/subscriptions?select=*`. Push is
`POST {url}/rest/v1/subscriptions?on_conflict=user_id,id` with
`Prefer: resolution=merge-duplicates,return=minimal`. Both send `apikey` and
`Authorization: Bearer <access token>`.

### 8.4 Failures and state

- Each request times out after 15 seconds and can be cancelled.
- Network errors, timeouts, HTTP 429, and HTTP 5xx retry with backoff: at most 3 attempts per sync
  pass, waiting 1 s and then 2 s. Other 4xx responses fail at once.
- The apps show one sync state: off (no project configured), signed out, syncing, synced at a
  time, or failed with a short reason. "Sync now" is always available while signed in.
- Automatic sync runs on launch and after each change while signed in; the desktop also syncs
  every 5 minutes.

## 9. Backup and restore

A backup is a full-fidelity JSON file the user saves wherever they like. Both apps read each
other's backups. `vectors/backup.json` pins validation and restore.

### 9.1 File

The suggested file name is `SubTrackr-backup-YYYYMMDD-HHMMSS.json` in local time.

```json
{
  "format": "subtrackr-backup",
  "formatVersion": 1,
  "exportedAt": "2026-09-26T10:00:00Z",
  "appVersion": "0.3.0",
  "platform": "desktop",
  "database": { "schemaVersion": "0.1", "settings": {}, "subscriptions": [] }
}
```

- `database` uses the proto3 JSON mapping (lowerCamelCase names, enum names as strings).
- Writers include default values and write 64-bit integers (`minorUnits`) as JSON strings.
  Readers accept those integers as strings or numbers, ignore unknown fields, and treat missing
  fields as proto defaults.
- The export holds every subscription, tombstones included, and all settings except `syncUrl` and
  `syncKey`. Sync configuration and the sign-in session never enter a backup.

### 9.2 Validation

Validation runs before anything changes and stops at the first failure:

| Code | When |
| --- | --- |
| `INVALID_JSON` | The text is not a JSON object, or is larger than 10 MB. |
| `UNSUPPORTED_FORMAT` | `format` is not `subtrackr-backup`. |
| `UNSUPPORTED_VERSION` | `formatVersion` is missing, not an integer, below 1, or above 1. |
| `INVALID_RECORD` | `database` is missing, or a subscription has an ID that is not a UUID, a duplicate ID, a currency that is not three letters A-Z, an exponent outside 0 to 4, or an empty `updatedAt`. |

### 9.3 Restore

- Merge (the default): subscriptions become `Merge(local, backup)` under the section 8.1 rules.
  Settings stay as they are. For each subscription in the backup the result counts it as `added`
  (the ID was not on the device), `updated` (the backup record won and differs in `updatedAt` or
  `deletedAt`), or `unchanged`.
- Replace: subscriptions and settings become the backup's, except that the device keeps its own
  `syncUrl` and `syncKey`. The apps ask for confirmation first. Every backup subscription counts as
  `added`.
- Both modes report `total`, the number of subscriptions (tombstones included) after the restore.
- The new database is built and checked in memory, then written with the atomic save. A failure
  leaves the existing file untouched.
- A later sync still merges with the cloud. Restoring with Replace does not delete rows in the
  cloud.

## 10. Themes

- `Settings.theme_mode` is `SYSTEM` (default), `LIGHT`, or `DARK`, stored per device and included
  in backups.
- `SYSTEM` follows the operating system and reacts when it changes while the app runs.
- Colors come from [`design/tokens.json`](design/tokens.json). Neutral tokens (surfaces, text,
  borders, status colors) and accent tokens are separate groups, so the accent can change without
  touching surfaces. Chart colors have their own list per theme.
- Each app has a test that compares its palettes with `tokens.json`.
- The desktop title bar follows the active theme.

## 11. Updates

Both apps update from the GitHub Releases of `lukr-99/SubTrackr`.
`vectors/release-selection.json` pins the choice.

- Discovery: `GET https://api.github.com/repos/lukr-99/SubTrackr/releases/latest`. GitHub never
  returns drafts or pre-releases there.
- Version: the tag is `vX.Y.Z`. An update is offered only when it is newer than the running version,
  comparing major, minor, and patch as numbers. A running version with a pre-release suffix such as
  `-dev` never checks.
- Artifacts: the desktop takes exactly `SubTrackr-Setup-X.Y.Z.exe`, Android exactly
  `SubTrackr-X.Y.Z.apk`, each with `<name>.sha256` next to it. The checksum file holds the
  lowercase hex SHA-256, optionally followed by whitespace and the file name. If either asset is
  missing, or a download URL is not `https://`, nothing is offered.
- Download: into app-private temporary storage, then verify the SHA-256. A mismatch deletes the file
  and reports a failure.
- Install: only after the user agrees. The desktop starts the installer and exits once it is
  running; Android hands the APK to the system installer, which also checks the signing key.
- Manual path: both apps link to the releases page.

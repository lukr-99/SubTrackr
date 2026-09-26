# SubTrackr architecture

## Context

SubTrackr tracks one person's subscriptions on Windows and Android. Each device keeps the full
data set locally and works offline. Sync is optional and runs against a Supabase project the user
owns; the repository and the builds contain no project, key, or account.

The two apps share no code. They share [`contracts/`](contracts/):

| Contract | Holds | Enforced by |
| --- | --- | --- |
| `proto/subtrackr.proto` | The data shape | C# code generation; the Kotlin mirror plus the vectors |
| `vectors/*.json` | Behavior: normalization, conversion, worth, merge, sync rows, backups, release selection | Both test suites load the same files |
| `design/tokens.json` | Theme colors | Both apps load it at runtime and test their palettes against it |
| `SPEC.md` | The prose both of the above trace back to | Review |

## Desktop (`desktop/`, WPF on .NET 10)

```text
SubTrackr.Desktop  -->  SubTrackr.Infrastructure  -->  SubTrackr.Core
        \___________________________________________________/^
```

- **`SubTrackr.Core`** (net10.0) holds the generated contracts, domain rules, and application
  services, with no file, HTTP, UI, or platform APIs. Folders: `Analytics/` (spend rollups),
  `Currency/` (rate tables), `Subscriptions/SubscriptionLedger` (the loaded database and every
  change to it), `Storage/` (the store port, seed data, load-time migrations), `Backup/`
  (envelope, validation, merge or replace), `Auth/` (sign-in state, session port), `Sync/`
  (merge engine, row mapping, retry policy, coordinator), `Updates/` (version policy, artifact
  selection, checksum parsing, update service), `Diagnostics/IAppLog`.
- **`SubTrackr.Infrastructure`** (net10.0-windows) implements the ports: `JsonDatabaseStore` and
  `AtomicFile`, `AppDataPaths`, `FrankfurterRateProvider`, `FileLog`, `BackupFiles`,
  `SupabaseAuthClient` and `SupabaseSubscriptionRemote`, `DpapiSessionStore`,
  `GitHubReleaseSource`, `VerifiedDownloader`, `InnoSetupLauncher`.
- **`SubTrackr.Desktop`** is the WPF shell: `Composition/`, `Shell/MainWindow`, `Views/`,
  `ViewModels/`, `Theming/`, `Controls/SpendChart` (drawn in `OnRender`, no chart library), and
  `Services/` for dialogs and other desktop seams.

**Composition root.** `App.OnStartup` reads `BuildInfo` (release or `-dev`), takes that build's
single-instance mutex, and calls `AppAdapters.ForUser(build)` to choose the real adapters. `AppGraph`
then builds the ledger, theme, rates, sync account and coordinator, backup and update services, and
the view models, all by constructor injection. `Start()` runs the launch sync, the rate refresh, the
update check, and the five-minute sync timer.

Tests mirror the three projects. UI tests render the real window off screen in both themes and fail
on binding errors.

## Android (`android/`, Kotlin and Compose)

```text
ui  -->  application  -->  domain  -->  model
 \          ^
  \         |
   '--> data (adapters implement application ports)
composition: the only place that picks adapters
```

`SubTrackrApplication` owns the `AppContainer` (the composition root). `MainActivity` takes the view
model factory from it, and view models get everything through their constructors. The package
layout, sync, backup, theme, and update details are in [android/README.md](android/README.md).

Screens have Robolectric and Roborazzi screenshots in both themes; `android/tools/` and
`android/.maestro/` hold the CodePrint emulator tooling and a launch smoke flow.

## Data flow

- Each app loads `data.json` into memory at start. Every change updates `updated_at` (or sets
  `deleted_at`) and saves the whole database through a temporary file and an atomic swap. An
  unreadable file is set aside, never overwritten.
- Totals, the worth verdicts, and the what-if calculator are pure functions over the loaded data
  and the current rate table.
- Rates come from Frankfurter on start and on request, with a built-in fallback table.
- Sync (optional): pull the user's rows, merge them with the local set (last writer wins,
  tombstones kept), save, push the merged set. Only subscriptions travel; settings stay local.
- Backup and restore go through a versioned JSON file that either app can read. Restore validates
  first and writes once.

## Server

`supabase/` holds the migrations for the `subscriptions` table. Rows are keyed by
`(user_id, id)`, and row-level security lets a signed-in user read, insert, and update only their
own rows; nothing deletes. The apps call the Auth and PostgREST REST endpoints directly. See
[docs/SYNC-SETUP.md](docs/SYNC-SETUP.md) and [ADR 0002](docs/adr/0002-per-user-sync-with-email-codes.md).

## Delivery

- One version for both apps in `Version.props`. Builds without the release flag are `X.Y.Z-dev`,
  keep separate data, and never check for updates.
- A `vX.Y.Z` tag runs the release workflow: the per-user Inno Setup installer and the signed APK,
  each with a SHA-256 file, into a draft GitHub Release that a person publishes.
- Both apps discover updates from `lukr-99/SubTrackr` releases, pick an exact asset name, and verify
  the SHA-256 before installing. The desktop updater is SubTrackr's own
  ([ADR 0001](docs/adr/0001-own-updater-instead-of-dotnetlib.md)).
- Details: [docs/releasing.md](docs/releasing.md).

## Known constraints

- The desktop installer is not Authenticode-signed yet; the published SHA-256 is the integrity
  check.
- Service logos come from Google's favicon service, which learns the website domains a user enters.
- Supabase's built-in mail sender allows only a few sign-in emails per hour.

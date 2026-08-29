# SubTrackr — Handoff

Everything needed to pick this project up on a **new machine**. Written 2026-08-28.
Repo: **https://github.com/lukr-99/SubTrackr** (private). Current version: **0.2.2**.

---

## 1. What this is

A personal **subscription tracker** for **Windows (WPF/.NET 10)** and **Android (Kotlin/Compose)**,
replacing a Notion page. Multi-currency, custom charts, a "worth it?" helper, free-trial tracking,
real service logos, and cross-device **sync via Supabase**. Two native apps that share **contracts,
not code**.

## 2. Current status — everything works

| Area | State |
|---|---|
| Contracts (`contracts/`) | ✅ protobuf data shape + golden vectors; C# 27 tests, Android vector tests, all green |
| Desktop (WPF) | ✅ installed as **0.2.2** (`%LOCALAPPDATA%\Programs\SubTrackr`, Start-menu searchable) |
| Android (Compose) | ✅ builds + runs on device; APK 0.2.2 installed |
| Sync (Supabase) | ✅ real relational `subscriptions` table, verified desktop↔cloud↔phone |
| Installer + release | ✅ Inno Setup per-user installer; public binary feed **v0.2.2** with .exe + .apk |

**Features:** add/edit/delete, dashboard totals, 3 switchable custom charts (donut/bars/trend),
multi-currency with live Frankfurter rates (+offline fallback), what-if calculator, worth-it verdict
with per-sub override (Auto/Essential/Always-worth/Not-worth), search + category filter + sortable
columns, auto-sync (launch + after every change), free-trial tracking + in-app alerts, real favicon
logos, dark theme, self-updater (via `DotNetLib.Core`).

## 3. Layout

```
contracts/           source of truth shared by BOTH apps (no app owns it)
  proto/subtrackr.proto     data-shape contract
  vectors/*.json            behavior contract (golden test fixtures)
  SPEC.md                   human-readable spec
desktop/             WPF / C# / .NET 10
  SubTrackr.Core/           domain + generated proto models (no UI)
  SubTrackr.Desktop/        WPF UI (MVVM, CommunityToolkit.Mvvm)
  SubTrackr.Core.Tests/     golden-vector runner (xUnit)
  local-packages/           vendored DotNetLib.Core.0.1.0.nupkg (so it builds standalone)
android/             Kotlin / Jetpack Compose (AGP 8.5.2, Kotlin 2.0.21, Compose BOM 2024.12)
installer/           Inno Setup script + build-installer.ps1
docs/SYNC-SETUP.md   Supabase setup (table SQL + steps)
ARCHITECTURE.md      component/dependency overview   (spec is contracts/SPEC.md)
```

Roadmap / next steps live in §7 of this file.

## 4. New-machine setup

### Prerequisites
- **.NET 10 SDK** (desktop). `dotnet --version` ≥ 10.
- **JDK 17+** (Android; 21 was used). Set `JAVA_HOME`.
- **Android SDK** with **platform android-35** + **build-tools 35.0.0** + platform-tools.
- **Inno Setup 6** (only to rebuild the installer) — https://jrsoftware.org/isdl.php
- **git**, **gh** (GitHub CLI, for releases).

### Clone
```bash
gh repo clone lukr-99/SubTrackr    # or: git clone https://github.com/lukr-99/SubTrackr
```

### Desktop — build & run
The desktop needs `DotNetLib.Core`, which is **vendored** in `desktop/local-packages/` and wired via
`nuget.config`, so it builds with no extra steps:
```bash
dotnet build SubTrackr.slnx
dotnet test  SubTrackr.slnx
dotnet run --project desktop/SubTrackr.Desktop
```
> If you later change `dotnetlib` itself, re-pack it (`dotnetlib/scripts/pack-local.ps1`) and drop the
> new `.nupkg` into `desktop/local-packages/`, or clone `dotnetlib` as a sibling (the `dotnetlib-local`
> source in `nuget.config` points at `..\dotnetlib\artifacts\nuget`).

### Rebuild the installer (optional)
```bash
./installer/build-installer.ps1            # needs Inno Setup 6; outputs installer/SubTrackr-Setup-<v>.exe
```

### Signed Android release + auto-update feed

The phone updater checks the public binaries-only repo `lukr-99/SubTrackr-Releases`. Every release
APK must use the same signing key or Android rejects in-place upgrades. This machine has the
gitignored key/config in `android/` and a second local copy under
`%USERPROFILE%\.android\SubTrackr\`. Back that directory up off-machine before moving builds.

```bash
cd android
./gradlew :app:assembleRelease
# app/build/outputs/apk/release/app-release.apk
```

Publish the APK and desktop installer on the same public-feed release. Neither artifact embeds the
device-local Supabase URL/key; those remain in app-private data across in-place upgrades.

### Android — build, test, run
1. Create **`android/local.properties`** (gitignored) — **use forward slashes**:
   ```
   sdk.dir=C:/Path/To/Android/Sdk
   ```
   (Backslashes break: `.properties` treats `\` as an escape → invalid path.)
2. Build / test / install:
   ```bash
   cd android
   JAVA_HOME=/path/to/jdk ./gradlew :app:testDebugUnitTest   # runs the shared golden vectors
   ./gradlew :app:assembleDebug                              # -> app/build/outputs/apk/debug/app-debug.apk
   ./gradlew :app:installDebug                               # to a connected device (USB debugging on)
   ```

### Sync (per device)
Sync config is **device-local** (stored in each device's data file, never in the repo or the synced
rows). On every new install:
1. Create a Supabase project (free) and run the SQL in **[docs/SYNC-SETUP.md](docs/SYNC-SETUP.md)** —
   it creates the `subscriptions` table + anon RLS policies.
2. In the app → **Settings → Sync**, paste the **Project URL** and the **Publishable key**
   (`sb_publishable_…`, the modern anon key), then it auto-syncs.
- Data files: desktop `%APPDATA%\SubTrackr\data.json`, Android app-private `files/data.json`.

## 5. How sync works (so you don't break it)

- Only **subscriptions** sync; settings (base currency, worth threshold, sync URL/key) stay local.
- **Last-writer-wins per row** by ISO-8601 UTC `updated_at` (lexical compare). Ties: a tombstone
  (`deleted_at` set) wins, else remote. Deletes are **soft** (never hard-delete). See `SPEC.md §8` and
  `contracts/vectors/merge.json`.
- A sync pass = **pull all rows → MergeEngine.Merge(local, remote) → save → bulk-upsert rows**.
  `SupabaseSyncProvider` (C# in `Core/Sync`, Kotlin in `domain`) maps DB rows ↔ `Subscription`.

## 6. Gotchas already hit (don't rediscover these)

- **`.slnx`** — .NET 10 uses the XML solution format, not `.sln`.
- **Android `local.properties`** — forward slashes only (see above).
- **Kotlin nested block comments** — `/*` inside a KDoc (e.g. a `*.json` glob) eats the closing `*/`.
- **Kotlin `var x` + `fun setX(...)`** — same JVM signature clash; rename the function.
- **WPF DatePicker** — its calendar parts ignore app-level implicit styles; scope them in a `Calendar`
  style attached via `DatePicker.CalendarStyle` (see `DarkCalendar` in `Themes/Dark.xaml`).
- **WPF TextBox** — center `PART_ContentHost` vertically or single-line text clips to the top.
- **First-run seed identity** — both apps use the fixed IDs in `contracts/vectors/seed-data.json`.
  Existing random-ID seed rows migrate to tombstones on load so sync cannot resurrect them.
- **Screenshots of a WPF window** — `PrintWindow(hwnd, hdc, 2)` is occlusion-proof; `CopyFromScreen`
  grabs whatever is on top. Android: `adb exec-out screencap -p > f.png` via a byte-clean shell
  (PowerShell `>` corrupts the PNG).

## 7. Open items / next steps

- **Worth threshold default (2.00)** is too low for CZK, so `AUTO` flags almost everything
  "not worth". Options: raise the default, make it currency-relative, or lean on the manual override.
- **What-if calculator** still judges by cost-per-use only; could add the worth-mode dropdown there.
- **OS-level notifications** — reminders are currently an in-app alerts card only; real desktop
  toast / Android scheduled notifications (WorkManager + POST_NOTIFICATIONS) are a follow-up.
- Feature backlog the user liked: budgets, payment-method/card tracking + expiry reminders,
  light-theme toggle, Android home-screen widget, CSV import/export, annual-vs-monthly savings hint.

## 8. Key facts

- GitHub: source repo **lukr-99/SubTrackr** is private. Desktop and Android updaters use the public,
  binaries-only **lukr-99/SubTrackr-Releases** feed so neither app stores a GitHub credential.
- License: PolyForm Noncommercial 1.0.0.
- Toolchain: .NET 10, WPF, CommunityToolkit.Mvvm, Google.Protobuf + Grpc.Tools (C# codegen);
  Kotlin 2.0.21, AGP 8.5.2, Compose BOM 2024.12, kotlinx.serialization, Coil, OkHttp.
- The `.proto` is the schema of record; C# generates from it, Kotlin **mirrors** it as
  `@Serializable` classes (kept in sync by hand + enforced by the shared vectors).

# SubTrackr

A subscription tracker for Windows and Android. It totals what you pay across currencies, tells
you which subscriptions earn their cost, warns about renewals and ending free trials, and can sync
your devices through a Supabase project you own.

Two native apps (WPF on .NET 10, Kotlin with Jetpack Compose) share no code. They share
[`contracts/`](contracts/): a protobuf data shape, golden test vectors that both test suites run,
the theme tokens, and [SPEC.md](contracts/SPEC.md).

## Features

- Monthly and yearly totals in a base currency, with ECB rates from Frankfurter and a built-in
  table for offline use.
- Donut, bar, and trend charts, a monthly budget bar, and a what-if calculator.
- A worth-it verdict from cost per use, with a manual override (Essential, Always worth, Not worth).
- Free-trial and renewal alerts, search, and a category filter.
- Service logos from each subscription's website, with a switch to turn them off.
- Light, dark, and system themes.
- Backup and restore through a JSON file that either app can read.
- Optional sync with email-code sign-in. Each user sees only their own rows; settings stay on each
  device.
- In-app updates from this repository's releases, verified by SHA-256.

## Install

Download the latest release from [Releases](https://github.com/lukr-99/SubTrackr/releases):

- Windows: `SubTrackr-Setup-X.Y.Z.exe` installs for the current user without admin rights.
- Android 8.0 or newer: `SubTrackr-X.Y.Z.apk`. Allow installs from your browser or file manager.

Each file has a `.sha256` next to it. Both apps check for newer releases themselves.

## Sync

Sync is off until you connect a Supabase project; nothing is sent anywhere before that. Setup,
the email template, and running Supabase locally are in [docs/SYNC-SETUP.md](docs/SYNC-SETUP.md).

## Data and privacy

- Windows keeps everything in `%APPDATA%\SubTrackr\data.json`, Android in its private app storage.
  Installer updates leave the data alone.
- Settings, Backup writes a full copy you can restore on either platform.
- Network use: Frankfurter for exchange rates, GitHub for update checks, your own Supabase project
  if you turn on sync, and Google's favicon service for service logos, which sees the website
  domains you enter. Turn service logos off in Settings and no favicon request is made.

## Build

```powershell
dotnet test SubTrackr.slnx
dotnet run --project desktop/SubTrackr.Desktop
cd android; .\gradlew.bat testDebugUnitTest assembleDebug
```

Local builds are `X.Y.Z-dev`: the desktop one keeps its data in `%APPDATA%\SubTrackr Dev`, and the
Android one installs beside the release as "SubTrackr Dev". Neither checks for updates.

| Guide | Covers |
| --- | --- |
| [docs/development.md](docs/development.md) | Toolchain, tests, emulator, known pitfalls |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Modules, data flow, delivery |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Checks and commit rules |
| [docs/releasing.md](docs/releasing.md) | Versions, signing, releases |
| [SECURITY.md](SECURITY.md) | Trust boundaries and reporting |

## License

[PolyForm Noncommercial 1.0.0](LICENSE.md). Third-party components are listed in
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

# SubTrackr

A subscription tracker for Windows (WPF) and Android (Kotlin/Compose). It totals what you pay
across currencies, tells you which subscriptions earn their cost, tracks free trials and upcoming
renewals, and syncs between your devices through a Supabase project you own.

The two apps share no code. They share `contracts/`: a protobuf data shape, golden test vectors
that both test suites run, and [SPEC.md](contracts/SPEC.md).

## Features

- Monthly and yearly totals in a base currency, with live ECB rates from Frankfurter and a built-in
  fallback table when offline.
- Donut, bar, and trend charts drawn by the apps themselves.
- A worth-it verdict from cost per use, with a manual override (Essential, Always worth, Not
  worth).
- A what-if calculator, a monthly budget bar, free-trial and renewal alerts, and service logos.
- Optional sync through your own Supabase project. Only subscriptions sync; settings stay on each
  device.

## Layout

```text
contracts/   shared data shape (proto), golden vectors, SPEC.md
desktop/     WPF app, .NET 10
android/     Kotlin/Compose app
supabase/    sync schema migrations
installer/   Inno Setup script and build script
docs/        setup and sync guides
```

## Build

```powershell
dotnet build SubTrackr.slnx
dotnet test SubTrackr.slnx
cd android; .\gradlew.bat testDebugUnitTest assembleDebug
```

[docs/development.md](docs/development.md) covers prerequisites and known pitfalls, and
[docs/SYNC-SETUP.md](docs/SYNC-SETUP.md) explains how to connect a Supabase project.

## Data

Each app keeps everything in one JSON file: `%APPDATA%\SubTrackr\data.json` on Windows and
`files/data.json` in app storage on Android. Installer updates leave it alone.

## License

[PolyForm Noncommercial 1.0.0](LICENSE.md).

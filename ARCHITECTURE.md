# Architecture

SubTrackr is two native apps that share **contracts, not code**:

- **Desktop** — C# / .NET 10 / **WPF**, MVVM (CommunityToolkit.Mvvm), a hand-rolled dark
  theme, JSON storage under `%AppData%`, and the shared **`DotNetLib.Core`** self-update service.
- **Android** *(later)* — Kotlin / Jetpack Compose, mirroring the same contracts.

They stay in lockstep through [`contracts/`](contracts/): a protobuf **data-shape** contract
(codegen for both languages) + **golden test vectors** for behavior + [`SPEC.md`](contracts/SPEC.md).

## Desktop layers

```
SubTrackr.Core         (net10.0, no UI)          SubTrackr.Desktop        (net10.0-windows, WPF)
├─ Contracts/          generated from .proto      ├─ App / MainWindow      bootstrap, single instance
├─ Normalization       billing → monthly-equiv    ├─ Services/AppState     load / compute / persist
├─ Currency/           rate table + Frankfurter    ├─ ViewModels/           MVVM (CommunityToolkit)
├─ Analytics/          SpendCalculator (rollups)   ├─ Controls/SpendChart   hand-rendered chart
├─ WorthIt             cost-per-use verdict        ├─ Views/                Add·Edit·Settings·WhatIf
└─ Storage/            JSON DataStore + seed       └─ Services/Updater,Log  DotNetLib.Core, logging
```

**Dependency rule:** `Desktop → Core → Contracts`. `Core` has no WPF/UI dependency, so all
domain logic is unit-testable headless (see `SubTrackr.Core.Tests`, which runs the golden vectors).

## Data & money

- The whole app state is one protobuf `Database` message, persisted as **one JSON file**
  (`%AppData%\SubTrackr\data.json`). That same document is what we will sync later.
- Money is integer minor-units + exponent (never a float). All money math is exact `decimal`;
  rounding happens only at display. See [`SPEC.md`](contracts/SPEC.md) §2–§4.

## Charts

`Controls/SpendChart` is a `FrameworkElement` that draws in `OnRender` with `DrawingContext` —
no third-party charting library. One `Slices` data source, three views (`Donut`, `Bars`, `Trend`).

## Self-update & packaging

- **Updater** wraps `DotNetLib.Core.Updating.UpdateService` pointed at the GitHub repo's
  public `releases/latest`; on launch it offers a newer build and downloads the installer.
- **Installer** is Inno Setup (per-user, no admin) built by `installer/build-installer.ps1`
  from a self-contained publish. Attach `SubTrackr-Setup-<v>.exe` to a GitHub Release; the
  updater finds it by the `.exe` asset.

## Threading

UI on the dispatcher thread; rate refresh and update checks are `async` and best-effort
(offline fallback for rates, null for updates). Single-instance is enforced with a named mutex.

# Development setup

## Toolchain

| Part | Needs |
| --- | --- |
| Desktop | .NET 10 SDK (`global.json` pins the feature band) |
| Android | JDK 17 or newer, Android SDK platform 35 and build-tools 35.0.0 |
| Installer | Inno Setup 6 (`ISCC.exe`), only to build the installer locally |
| Releases | `gh` (GitHub CLI) |

## Desktop

```powershell
dotnet build SubTrackr.slnx
dotnet test SubTrackr.slnx
dotnet run --project desktop/SubTrackr.Desktop
```

C# models are generated from `contracts/proto/subtrackr.proto` at build time through Grpc.Tools.
Never edit the generated files.

Desktop UI tests render the real window off screen at (-30000, -30000) in both themes and fail
on any binding error. Set `SUBTRACKR_SCREENSHOTS` to a folder to keep the PNGs:

```powershell
$env:SUBTRACKR_SCREENSHOTS = "$env:TEMP\subtrackr-shots"; dotnet test desktop/SubTrackr.Desktop.Tests
```

The app icon (`desktop/SubTrackr.Desktop/Assets/SubTrackr.ico`) is rendered from
`contracts/design/logo.json` and the `brand` colors in `tokens.json`. After changing either, run
`powershell -ExecutionPolicy Bypass -File toolsender-desktop-icon.ps1` (Windows PowerShell 5.1,
or `pwsh -STA`) and commit the `.ico`; `-PngFolder <dir>` also writes each frame as a PNG to review.
The Android launcher and splash icons are vector drawables checked against the same files by
unit tests.

A debug build (`X.Y.Z-dev`) keeps its data in `%APPDATA%\SubTrackr Dev` and runs beside an
installed release.

## Android

Create `android/local.properties` (git-ignored) with forward slashes:

```properties
sdk.dir=C:/Path/To/Android/Sdk
```

A backslash path breaks, because `.properties` files treat `\` as an escape.

```powershell
cd android
.\gradlew.bat testDebugUnitTest   # includes the shared golden vectors
.\gradlew.bat assembleDebug       # app/build/outputs/apk/debug/
.\gradlew.bat installDebug        # to the one connected device or emulator
```

This loop covers most Android work without a phone:

- `android/tools/agent-doctor.ps1` checks the SDK, emulator, and Maestro setup.
- `android/tools/emulator.ps1` starts a headless emulator; `build-and-install.ps1` and
  `ui-check.ps1` install the debug build and capture the screen and UI tree.
- `maestro test android/.maestro/launch-smoke.yaml` runs the launch smoke flow.
- `.\gradlew.bat recordRoborazziDebug` rewrites the JVM screenshots in
  `app/src/test/screenshots/`; `verifyRoborazziDebug` compares against them. The references were
  recorded on Windows, so verify there. Plain `testDebugUnitTest` renders without comparing.
- Robolectric for SDK 35 runs on JDK 17 or 21. If a machine-wide Gradle property pins another JDK,
  pass `"-Dorg.gradle.java.home=<jdk>"`.
- Debug builds may talk to a local Supabase stack at `http://10.0.2.2:54621`
  ([docs/SYNC-SETUP.md](SYNC-SETUP.md#running-supabase-locally)).

## Gotchas already paid for

- `.slnx` is the .NET 10 XML solution format. There is no `.sln`.
- Kotlin block comments nest. A `/*` inside KDoc, such as a `*.json` glob, swallows the closing
  `*/`.
- A Kotlin `var x` with a private setter clashes with a function named `setX(...)` on the JVM.
  Rename the function.
- WPF `DatePicker` ignores app-level implicit styles for its calendar parts. Scope them in a
  `Calendar` style and attach it through `DatePicker.CalendarStyle`.
- A WPF `TextBox` template must center `PART_ContentHost` vertically, or single-line text clips.
- An app-wide implicit `TextBlock` style also recolors text inside control templates, so it must
  not set a foreground color.
- `System.Windows.ThemeMode` clashes with the proto's `ThemeMode`; qualify one of them.
- WPF `ProgressBar.Value` binds two-way by default. Use `Mode=OneWay` for read-only view-model
  properties.
- First-run seed rows use the fixed IDs in `contracts/vectors/seed-data.json`. Rows seeded with
  random IDs by old builds become tombstones on load so sync cannot bring them back.
- Capture Android screenshots with `adb exec-out screencap -p > shot.png` from a byte-clean shell.
  Windows PowerShell 5.1 redirection corrupts the PNG.

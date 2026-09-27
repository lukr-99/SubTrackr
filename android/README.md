# SubTrackr for Android

Kotlin/Jetpack Compose app. It mirrors the desktop app through the shared
[`contracts/`](../contracts): `model/` mirrors the proto as `@Serializable` classes with the same
JSON field names, and the unit tests load the same golden vectors as the desktop tests.

```text
app/src/main/kotlin/com/lukr99/subtrackr/
  SubTrackrApplication.kt  process entry point, owns the AppContainer
  MainActivity.kt          host activity
  composition/             AppContainer (composition root) and the view-model factory
  model/                   one data class or enum per proto type
  domain/                  pure Kotlin: currency, spend, worth, sync merge, update policy
  application/             use cases (AppRepository, UpdateService) and the ports adapters implement
  data/                    adapters: JSON store, seed data, Frankfurter, Supabase, GitHub Releases
  ui/                      Compose app shell, screens by feature, components, theme
app/src/test/kotlin/       unit and golden-vector tests, mirroring the main packages
```

`domain` depends on nothing but `model`. Adapters in `data` implement the ports declared in
`application`. Only `composition/` picks adapters and builds the object graph.

## Data and backups

`data.json` in the app's private files holds the whole `Database`; every change replaces it with an
atomic rename. Settings > Backup exports a cross-platform JSON file (SPEC.md section 9) through the
system file picker and restores one with Merge (the default) or Replace after a confirmation. A
restore validates the file and builds the result in memory before the single atomic save, so a bad
file or a failed save changes nothing. Backups never contain the sync URL, key, or sign-in session.

## Crash log

`SubTrackrApplication` installs an uncaught-exception handler before anything else. It appends
one entry per crash to `files/crash.log` (time, app version, thread, and each exception in the
cause chain as its class and stack frames), then hands the crash to the handler Android had
before. Exception messages are left out because they can carry names, addresses, or tokens. The
file stays under 64 KB by dropping the oldest entries. On the debug build,
`.\tools\pull-debug-files.ps1 -Destination artifacts\device-files -FilePattern '\.log$'` copies it
off an emulator.

## Sync

Settings > Sync takes a Supabase project URL and publishable key, then signs in with an emailed
code (SPEC.md section 8). The session lives only in `noBackupFilesDir/sync-session.bin`, sealed with
AES-GCM under an Android Keystore key; it never enters data.json, backups, or logs. Sync runs on
launch and after every change while signed in, and on "Sync now". Each request times out after 15
seconds; network errors, timeouts, 429, and 5xx get up to three attempts per pass. Changing the URL
or key signs out. Release builds accept only `https://` projects; debug builds may also use
`http://10.0.2.2` or `http://127.0.0.1` for a local Supabase stack (debug-only network security
config).

## Themes

Settings offers System, Light, and Dark. Every color comes from
[`contracts/design/tokens.json`](../contracts/design/tokens.json), which Gradle puts on the main
Java resources; `ThemeTokensTest` fails when the app's palettes and the file disagree.

## Service logos

The dashboard shows each subscription's website favicon from Google's favicon service, with the
emoji while it loads, when it fails, or when there is no website (SPEC.md section 12). Settings >
Appearance > "Show service logos" stores `hideServiceLogos` (default off, so logos show); with logos
hidden, `ServiceIcon` never hands Coil a request. The setting is per device and goes into backups.

## Updates

Release builds check `lukr-99/SubTrackr` on launch and from Settings. They take only
`SubTrackr-X.Y.Z.apk` with its `.sha256` file, download it into the app's cache, verify the SHA-256,
and then hand it to the system installer. Debug builds are `X.Y.Z-dev` and never check. Settings
also links to the releases page for a manual install.

## Logo and launch

The launcher icon, its Android 13 themed (monochrome) layer, and the splash icon are vector
drawables mapped from [`contracts/design/logo.json`](../contracts/design/logo.json) and the `brand`
colors in `tokens.json`: the launcher bars at 72/256 scale with an 18 dp offset on the 108 dp canvas,
the splash mark at 56/256 with a 26 dp offset. `LauncherIconTest` and `SplashIconTest` fail when
the drawables drift from those files.

`core-splashscreen` shows the mark at launch. From Android 12 the bars grow in one after another
(`drawable-v31/splash_icon.xml`); earlier versions show the still mark. On a cold start the splash
stays until the 540 ms animation has played, never on a warm start or with animations off. The
splash follows the system's light or dark setting; the app applies its own theme choice right
after.

## Screenshots

Robolectric and Roborazzi render the main screens in light and dark on the JVM. `testDebugUnitTest`
runs them as rendering checks; `recordRoborazziDebug` rewrites the reference images in
`app/src/test/screenshots/`, and `verifyRoborazziDebug` compares against them. The references were
recorded on Windows, so verify on Windows. The whole suite, screenshots included, runs on JDK 17
and on JDK 21.

## Checking on an emulator

`tools/` holds the CodePrint Android scripts (from `android/`, Windows PowerShell 5.1 or 7). They
only ever act on the debug build, `com.lukr99.subtrackr.dev`, and prefer an emulator when a phone
is also attached. Output goes to the ignored `artifacts/` folder.

```powershell
.\tools\agent-doctor.ps1                       # read-only toolchain check
.\tools\emulator.ps1 start -DisableAnimations  # headless, prints the emulator serial
.\tools\ui-check.ps1 -Flow .maestro            # build, install, launch, screenshot, Maestro flows
.\tools\emulator.ps1 stop
```

`.maestro/launch-smoke.yaml` launches the app, waits for the dashboard, and visits every tab by
the Compose test tags, which the app root exposes as resource ids.

Build and test steps are in [docs/development.md](../docs/development.md).

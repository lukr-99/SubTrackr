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

## Themes

Settings offers System, Light, and Dark. Every color comes from
[`contracts/design/tokens.json`](../contracts/design/tokens.json), which Gradle puts on the main
Java resources; `ThemeTokensTest` fails when the app's palettes and the file disagree.

## Updates

Release builds check `lukr-99/SubTrackr` on launch and from Settings. They take only
`SubTrackr-X.Y.Z.apk` with its `.sha256` file, download it into the app's cache, verify the SHA-256,
and then hand it to the system installer. Debug builds are `X.Y.Z-dev` and never check. Settings
also links to the releases page for a manual install.

## Screenshots

Robolectric and Roborazzi render the main screens in light and dark on the JVM. `testDebugUnitTest`
runs them as rendering checks; `recordRoborazziDebug` rewrites the reference images in
`app/src/test/screenshots/`, and `verifyRoborazziDebug` compares against them. The references were
recorded on Windows, so verify on Windows. Robolectric's SDK 35 runtime needs JDK 21.

Build and test steps are in [docs/development.md](../docs/development.md).

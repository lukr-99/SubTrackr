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

## Updates

Release builds check `lukr-99/SubTrackr` on launch and from Settings. They take only
`SubTrackr-X.Y.Z.apk` with its `.sha256` file, download it into the app's cache, verify the SHA-256,
and then hand it to the system installer. Debug builds are `X.Y.Z-dev` and never check. Settings
also links to the releases page for a manual install.

Build and test steps are in [docs/development.md](../docs/development.md).

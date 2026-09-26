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
  domain/                  pure Kotlin: currency, spend, worth, sync merge
  application/             use-case state (AppRepository) and the ports adapters implement
  data/                    adapters: JSON store, seed data, Frankfurter rates, Supabase, updater
  ui/                      Compose app shell, screens by feature, components, theme
app/src/test/kotlin/       unit and golden-vector tests, mirroring the main packages
```

`domain` depends on nothing but `model`. Adapters in `data` implement the ports declared in
`application`. Only `composition/` picks adapters and builds the object graph.

Build and test steps are in [docs/development.md](../docs/development.md).

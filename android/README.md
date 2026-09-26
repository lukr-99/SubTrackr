# SubTrackr for Android

Kotlin/Jetpack Compose app. It mirrors the desktop app through the shared
[`contracts/`](../contracts): `model/` mirrors the proto as `@Serializable` classes with the same
JSON field names, and the unit tests load the same golden vectors as the desktop tests.

```text
app/src/main/kotlin/com/lukr99/subtrackr/
  model/    data classes mirroring the proto
  domain/   normalization, currency, worth-it, spend, merge (pure Kotlin)
  data/     JSON store, seed data, repository
  ui/       Compose screens and theme
  update/   GitHub Releases updater
app/src/test/kotlin/   golden-vector and unit tests
```

Build and test steps are in [docs/development.md](../docs/development.md).

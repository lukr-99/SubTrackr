# SubTrackr — Android

Kotlin / Jetpack Compose app, mirroring the desktop (WPF) app through the shared
[`contracts/`](../contracts). Baselined on the same toolchain as the sibling
Relay / workout-tracker apps: **AGP 8.5.2, Kotlin 2.0.21, Compose BOM 2024.12, Gradle 8.9**,
compileSdk 35, minSdk 26.

## Shared contract

- **Data shape** — `model/Contracts.kt` mirrors [`contracts/proto/subtrackr.proto`](../contracts/proto/subtrackr.proto)
  as `@Serializable` classes (JSON field names match the proto, so storage is sync-compatible with WPF).
- **Behaviour** — the unit tests run the **same** golden vectors as the WPF app
  (`contracts/vectors/*.json`, put on the test classpath). Parity is enforced: if the Kotlin
  math drifts from the spec, these tests go red.

```
app/src/main/kotlin/com/lukr99/subtrackr/
  model/        data classes mirroring the proto
  domain/       Normalization, Currency, WorthIt, SpendCalculator (pure Kotlin)
  data/         SeedData + JSON SubscriptionStore (%files%/data.json)
  ui/           Compose theme, Format, DashboardScreen
  MainActivity.kt
app/src/test/kotlin/...   golden-vector tests (shared fixtures)
```

## Build & test

```bash
# unit tests (runs the shared golden vectors)
./gradlew :app:testDebugUnitTest

# debug APK -> app/build/outputs/apk/debug/app-debug.apk
./gradlew :app:assembleDebug

# install on a connected device
./gradlew :app:installDebug
```

Requires the Android SDK (path in `local.properties`, gitignored) and a JDK 17+.

## Status

Phase 6 foundation: project builds, domain + parity tests green, dashboard renders
(totals, donut, subscription list). Next: add/edit, currency picker, charts switcher,
worth-it + what-if screens to match the desktop feature set.

# SubTrackr

A personal subscription tracker for **Windows (WPF)** and **Android (Kotlin)** —
multi-currency, custom charts, and a "worth it?" decision helper. Private, versioned,
auto-updating.

## Layout

```
contracts/           # source of truth shared by BOTH apps (no app owns it)
  proto/             #   data-shape contract (protobuf) -> codegen C# + Kotlin
  vectors/           #   behavior contract (golden test vectors)
  SPEC.md            #   human-readable spec
desktop/             # WPF / C# / .NET 10 app
  SubTrackr.Core/    #   domain logic + generated models (no UI deps)
  SubTrackr.Desktop/ #   WPF UI (MVVM)
android/             # Kotlin / Jetpack Compose app (later phase)
```

## Status

Phase 0 — foundation. See [`contracts/SPEC.md`](contracts/SPEC.md) and the
roadmap in the project chat.

## Desktop — build & run

```bash
dotnet build SubTrackr.slnx
dotnet run --project desktop/SubTrackr.Desktop
```

Requires the .NET 10 SDK. C# models are generated from `contracts/proto/*.proto`
at build time (via Grpc.Tools) — do not edit generated model files by hand.

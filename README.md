# SubTrackr

A personal subscription tracker for **Windows (WPF)** and **Android (Kotlin)** —
multi-currency, custom charts, a "worth it?" decision helper, free-trial tracking,
real service logos, and cross-device sync via Supabase. Private, versioned, auto-updating.

> **Picking this up on another machine? Start with [HANDOFF.md](HANDOFF.md).**

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

**v0.2.0 — feature-complete and running on real hardware** (desktop installed, Android on device,
Supabase sync live). Full status, setup on a new machine, gotchas, and next steps are in
[HANDOFF.md](HANDOFF.md). Spec: [`contracts/SPEC.md`](contracts/SPEC.md).

## Desktop — build & run

```bash
dotnet build SubTrackr.slnx
dotnet run --project desktop/SubTrackr.Desktop
```

Requires the .NET 10 SDK. C# models are generated from `contracts/proto/*.proto`
at build time (via Grpc.Tools) — do not edit generated model files by hand.

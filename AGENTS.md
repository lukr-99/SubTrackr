# AGENTS

SubTrackr follows the CodePrint baseline. Two native apps (WPF desktop, Kotlin Android) share
contracts, not code.

## Read first

1. [ARCHITECTURE.md](ARCHITECTURE.md) before changing module seams or dependency direction.
2. [contracts/SPEC.md](contracts/SPEC.md) before changing behavior either app implements.
3. [CONTRIBUTING.md](CONTRIBUTING.md) for the verification commands and commit rules.
4. [docs/development.md](docs/development.md) for toolchain setup and known gotchas.

## Rules that bite here

- Behavior both apps implement changes in `contracts/` first (spec, then vectors), then in both
  apps in the same change. Never keep translated copies of a vector file.
- One top-level type per file, named after the type. A XAML view and its code-behind are the one
  exception.
- Domain code (`SubTrackr.Core`, Android `domain/`) does not touch UI, files, HTTP, or platform
  APIs. Adapters live in `SubTrackr.Infrastructure` and Android `data/`.
- Supabase migrations in `supabase/migrations/` are immutable once applied. Add the next number and
  its isolated-test fixture; never edit an old file.
- Nothing personal goes into the repository: no Supabase URLs or keys, device serials, machine
  paths, signing material, or real subscription data. Sample data stays generic.
- Debug builds are `X.Y.Z-dev`, keep their data apart from the installed app, and never check for
  updates.
- Conventional Commits, one coherent change per commit, no AI co-author trailers.

## Verify before finishing

```powershell
python tools/validate_repository.py --root .
dotnet format SubTrackr.slnx --verify-no-changes
dotnet test --solution SubTrackr.slnx -c Release
cd android; .\gradlew.bat testDebugUnitTest lintDebug assembleDebug
```

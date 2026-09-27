# Contributing

## Local verification

Run these from the repository root before you push. CI runs the same checks.

```powershell
python tools/validate_repository.py --root .
dotnet format SubTrackr.slnx --verify-no-changes
dotnet build SubTrackr.slnx -c Release
dotnet test SubTrackr.slnx -c Release
```

Android, from `android/`:

```powershell
.\gradlew.bat spotlessCheck testDebugUnitTest lintDebug assembleDebug
```

Setup, SDK paths, and the gotchas that already cost time are in
[docs/development.md](docs/development.md).

## Change shape

- Use Conventional Commits: `type(optional-scope): imperative summary`. Scopes in use are
  `desktop`, `android`, `contracts`, `sync`, `backup`, `updates`, `theme`, `ci`, `release`.
- Keep each commit to one behavior or repository change, with its tests and docs.
- Behavior that both apps implement starts in `contracts/`: update `SPEC.md`, then the golden
  vectors, then both apps. A vector change that only one app passes is not done.
- Add user-visible changes to `CHANGELOG.md` under `[Unreleased]` in the same commit.
- Do not commit build output, installers, APKs, secrets, signing files, `local.properties`,
  machine paths, device serials, or real personal data.

## Pull requests

State what changed, how you verified it, screenshots for UI work, and the data or migration
impact. Risky delivery changes also need a rollback note. CI must be green before merge.

## Releases

See [docs/releasing.md](docs/releasing.md).

# Own updater instead of dotnetlib

## Context

CodePrint asks WPF apps to take shared primitives, including the updater, from `dotnetlib`
first. SubTrackr used `DotNetLib.Core`'s `UpdateService`, restored from a vendored package folder
and an optional sibling feed. `dotnetlib` is private and has no license yet, so a public clone of
SubTrackr and GitHub's CI cannot restore it. SubTrackr also picked the installer by its `.exe`
suffix, while SPEC section 11 now pins exact asset names and a published SHA-256 next to each
installer.

## Decision

SubTrackr drops `DotNetLib.Core` and restores from nuget.org only. The desktop app has its own
updater, split along the CodePrint seams: `IReleaseSource` (GitHub `releases/latest`),
`VersionPolicy` and `ArtifactSelector` (pure, in Core, checked against
`contracts/vectors/release-selection.json`), `IUpdateDownloader` (download to a private temporary
folder with a SHA-256 check), and `IInstallerLauncher` (Inno Setup, silent). `UpdateService`
coordinates them; the UI only asks for consent.

## Why

The updater is small, and the spec pins the release repository, exact asset names, HTTPS, and
checksum verification, which both apps implement against the same vectors. Owning it keeps the
repository buildable in public. Revisit this when `dotnetlib` publishes a licensed package to a
public feed with the same guarantees.

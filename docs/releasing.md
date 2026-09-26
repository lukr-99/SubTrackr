# Releasing

A release is a `vX.Y.Z` tag on `main` whose number matches `Version.props`. The release workflow
builds the desktop installer and the signed APK from that tag and drafts a GitHub Release. Nothing
reaches users until someone publishes the draft.

## Versioning

- `Version.props` holds the next release's number for both apps. Bump it right after a release, or
  in the release commit if it was not bumped yet.
- Major: a change that breaks stored data or the backup format. Minor: new features. Patch: fixes
  only. Before 1.0, a change that needs a server migration is a minor bump.
- Local builds are `X.Y.Z-dev`. The desktop dev build keeps its data in `%APPDATA%\SubTrackr Dev`,
  the Android debug build installs as `com.lukr99.subtrackr.dev` ("SubTrackr Dev"), and neither
  checks for updates.
- Android `versionCode` is `major * 10000 + minor * 100 + patch`, computed by Gradle.

## Assets

| File | Built by |
| --- | --- |
| `SubTrackr-Setup-X.Y.Z.exe` and `.sha256` | `installer/build-installer.ps1` on `windows-latest` |
| `SubTrackr-X.Y.Z.apk` and `.sha256` | `gradlew assembleRelease` on `ubuntu-latest`, signed in CI |

The apps look for exactly these names in the latest published release and verify the SHA-256
before installing (SPEC section 11).

## Steps

1. Start from a clean `main` with CI green.
2. In one commit, `chore(release): X.Y.Z`: set `Version.props` if needed and rename
   `## [Unreleased]` in `CHANGELOG.md` to `## [X.Y.Z] - YYYY-MM-DD`, with a fresh empty
   `[Unreleased]` above it and the compare links updated.
3. Try the desktop installer locally:

   ```powershell
   .\installer\build-installer.ps1
   .\installer\dist\SubTrackr-Setup-X.Y.Z.exe
   ```

   Check a fresh install, an upgrade over the previous version (data in `%APPDATA%\SubTrackr`
   survives), and uninstalling (program files go, the data folder stays).
4. Tag and push:

   ```powershell
   git tag -a vX.Y.Z -m "SubTrackr X.Y.Z"
   git push origin main vX.Y.Z
   ```

5. The workflow checks the tag against `Version.props` and `CHANGELOG.md`, runs both test suites,
   builds both assets with their checksums, checks that the APK's signing certificate matches
   `android/release-signing-cert.sha256`, and drafts the release with the changelog section as
   notes.
6. Download the draft's assets, compare their hashes with the `.sha256` files, and publish the
   draft. Both apps' update checks see it from then on.

## Android signing

The release key lives outside Git: `android/subtrackr-release.jks` plus `android/keystore.properties`
on the release machine, a backup copy, and four repository secrets that the workflow uses
(`SUBTRACKR_KEYSTORE_BASE64`, `SUBTRACKR_KEYSTORE_PASSWORD`, `SUBTRACKR_KEY_ALIAS`,
`SUBTRACKR_KEY_PASSWORD`). `tools/setup-android-signing.ps1` creates the key with a generated
password, backs it up and verifies the copy, and with `-UploadSecrets` sets the secrets through a
temporary env file. Keep one copy of the key and its password offline.

`android/release-signing-cert.sha256` pins the certificate. A tag without the secrets, or with a
different key, fails the workflow and publishes nothing.

Up to 0.2.2 the APK was signed with a development key. Android refuses to update an app signed
with a different key, so moving from 0.2.x to 0.3.0 on a phone means: back up or sync, uninstall,
install 0.3.0, then restore or sign in. From 0.3.0 on, updates install in place.

## First install and the manual path

- Windows: run `SubTrackr-Setup-X.Y.Z.exe`. It installs for the current user without admin rights.
- Android: download `SubTrackr-X.Y.Z.apk` from the release, allow installs from that source, and
  install.
- Both apps link to the releases page, so a manual download always works if the in-app updater
  fails.

## Recovery

- A wrong tag: delete the draft and the tag (`git push origin :refs/tags/vX.Y.Z`), fix, tag again.
- A bad published release: publish a patch release. On Windows, installing the previous installer
  over it also works; data is not touched.
- The installer is not Authenticode-signed yet. When a certificate exists, sign the installer
  before hashing it and run the build with `-RequireSigned`.

# Changelog

All notable changes to SubTrackr. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Light, dark, and system themes on both apps, from one shared color file. The desktop title bar
  follows the theme.
- Backup and restore on both apps: a JSON file either app can read, restored by merging (the
  default) or replacing after a confirmation, with a count of what changed.
- Email-code sign-in for sync. Sync shows its state (off, signed out, syncing, synced at a time,
  or failed) next to Sync now and Sign out.
- Settings links to the releases page, and the desktop app checks for updates on request.
- CI for the repository baseline, Supabase migrations and row security, and both apps, plus a
  tag-driven release that drafts both installers with SHA-256 files.

### Changed

- Sync is per user: each signed-in user reads and writes only their own rows, and the anonymous
  key alone can no longer read or change anything. A Supabase project needs migration
  `0002_per_user_rows.sql` and the sign-in email template (docs/SYNC-SETUP.md); versions up to
  0.2.2 stop syncing once it runs.
- Both apps update from this repository's releases, pick exact file names, and check the SHA-256
  before installing. The desktop app no longer depends on the private DotNetLib.Core package.
- The Android APK is signed with a dedicated release key instead of a development key. Moving a
  phone from 0.2.x needs one uninstall and reinstall; back up or sync first.
- Both apps take their version from one `Version.props`. Local and debug builds are `X.Y.Z-dev`,
  keep their own data (`%APPDATA%\SubTrackr Dev`, "SubTrackr Dev" on Android), and never check for
  updates.
- Muted text and dark-theme buttons reach AA contrast.
- The first-run sample subscriptions are generic examples.

### Fixed

- A `data.json` that cannot be read is kept as `data.json.unreadable-<time>` instead of being
  overwritten (Android) or stopping the app from starting (Windows).
- A data file written by a newer version still opens.
- Unspecified or zero-day billing cycles no longer break totals on Android.

## [0.2.2] - 2026-08-29

### Fixed

- The first sync between two devices no longer duplicates the sample subscriptions. Both apps seed
  the same fixed IDs, and rows from older random-ID seeds turn into tombstones.
- Startup sync no longer waits for the exchange-rate refresh.

### Added

- Android checks GitHub Releases for a newer APK and offers to install it.

## [0.2.1] - 2026-08-29

### Added

- Monthly budget with a progress bar on the dashboard, stored per device.
- Numbered Supabase migrations under `supabase/migrations/`.

### Changed

- The worth threshold is saved per device and defaults to a value that suits the base currency
  (for example 35 for CZK instead of 2).

## [0.2.0] - 2026-08-28

### Added

- Manual worth override: Auto, Essential, Always worth, Not worth.
- Search, category filter, and sorting for the subscription list.
- Automatic sync on launch and after every change.
- Free-trial end dates, an alerts card for upcoming renewals and trials, and service logos.

### Changed

- Supabase stores one typed row per subscription instead of a single JSON document. Sync needs the
  new table.

### Fixed

- Desktop text boxes no longer clip single-line text.
- Android back navigation, status bar insets, and the launcher icon.

## [0.1.0] - 2026-08-28

### Added

- Desktop app: dashboard with totals, donut, bar, and trend charts, multi-currency with live rates,
  what-if calculator, worth-it verdict, and an Inno Setup installer.

[Unreleased]: https://github.com/lukr-99/SubTrackr/compare/v0.2.2...HEAD
[0.2.2]: https://github.com/lukr-99/SubTrackr/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/lukr-99/SubTrackr/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/lukr-99/SubTrackr/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/lukr-99/SubTrackr/releases/tag/v0.1.0

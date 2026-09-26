# Changelog

All notable changes to SubTrackr. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed

- Both apps take their version from one `Version.props`. Local and debug builds are `X.Y.Z-dev`;
  the Android debug build installs beside the release as "SubTrackr Dev".
- The installer build writes `installer/dist/SubTrackr-Setup-X.Y.Z.exe` with a `.sha256` file and
  waits for a running SubTrackr to close.

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

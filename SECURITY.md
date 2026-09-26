# Security

## Supported versions

Only the latest release gets fixes. Older builds should update through the app or by installing
the latest release by hand.

## Reporting a vulnerability

Use GitHub's private vulnerability reporting on this repository (Security tab, "Report a
vulnerability"). Please do not open a public issue for something that is still exploitable.

## Trust boundaries

| Boundary | What crosses it | Protection |
| --- | --- | --- |
| Supabase sync | Subscription rows between a device and the user's own Supabase project | HTTPS (plain HTTP only to a loopback or emulator host in debug builds). Email-code sign-in; row-level security limits every read and write to the signed-in user; the anonymous role has no access. The repository and builds contain no project URL or key. |
| Sign-in session | Access and refresh tokens | Stored only in a DPAPI-protected file (Windows, current user) or an AES-GCM file under an Android Keystore key in no-backup storage. Never in `data.json`, backups, or logs. Changing the project or a rejected refresh signs out. |
| Updates | Release metadata, installers, and APKs from GitHub Releases | HTTPS only, exact asset names, SHA-256 verified before anything runs, and the user agrees first. Android's installer also checks the APK's signing key, and the release workflow refuses an APK signed by any other certificate. |
| Backup files | JSON the user picks | Size-limited, validated in full (format, version, IDs, currency, exponent) before anything changes; written in one atomic save. Backups never hold the sync project or the session. |
| Exchange rates | Rate tables from the Frankfurter API | HTTPS, read-only. A failure falls back to the built-in table. |
| Service logos | Favicons for the website a user entered | Fetched from Google's favicon service over HTTPS, so Google learns those domains. Display only. |
| Local data | `data.json` in the user's app data folder | Operating-system file permissions. |

## Recovery

- Leaked publishable key: rotate it in the Supabase dashboard and enter the new key on each
  device. The key alone reads nothing.
- Lost device: delete the user's sessions under Authentication, Users in Supabase (or the user),
  which revokes its refresh token.
- Bad update: install the previous release from GitHub Releases over it. Data lives outside the
  install folder and survives.
- Damaged `data.json`: the app keeps it as `data.json.unreadable-<time>` and starts fresh; restore
  a backup or sign in to sync.
- Deleting your data: remove the app data folder (`%APPDATA%\SubTrackr` on Windows, app storage on
  Android) and delete your user in the Supabase project, which removes your rows.

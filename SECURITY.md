# Security

## Supported versions

Only the latest release gets fixes. Older builds should update through the app or by installing
the latest release by hand.

## Reporting a vulnerability

Use GitHub's private vulnerability reporting on this repository (Security tab, "Report a
vulnerability"). Please do not open a public issue for something that is still exploitable.

## Trust boundaries

| Boundary | What crosses it | Current protection |
| --- | --- | --- |
| Supabase sync | Subscription rows between a device and the user's own Supabase project | HTTPS. Each user brings their own project; the repository and the builds contain no project URL or key. Row security is anonymous today, see the known gap below. |
| Exchange rates | Rate tables from the Frankfurter API | HTTPS, read-only. A bad response falls back to the built-in table. |
| Service logos | Favicon images for the domain a user typed | HTTPS, display only. |
| Updates | Release metadata and installers or APKs from GitHub Releases | HTTPS. The desktop installer runs only after the user agrees. The Android system installer checks that the APK is signed by the same key. |
| Local data | `data.json` in the user's app data folder | Operating-system file permissions. |

## Known gap

Sync still uses Supabase's anonymous role with permissive row security, so anyone holding both a
project's URL and its publishable key can read and change that project's rows. Keep both private.
Authenticated per-user row security replaces this in the next release.

## Recovery

- Lost or leaked Supabase key: rotate the publishable key in the Supabase dashboard, then enter the
  new key under Settings, Sync on each device.
- Bad update: install the previous release from GitHub Releases over it. Data lives outside the
  install folder and survives.
- Deleting your data: remove the app data folder (`%APPDATA%\SubTrackr` on Windows, app storage on
  Android) and the rows in your Supabase project.

# Per-user sync with email codes

Up to 0.2.2, sync ran as Supabase's anonymous role with policies that allowed every read and write.
Anyone with a project's URL and publishable key could read and change all of its rows, and CodePrint
does not accept a public key as authorization. With the repository public, more people may point the
apps at their own projects, so the gap matters beyond one owner.

Sync now requires Supabase Auth. The apps sign in with a 6-digit email code through the Auth REST
API, the same flow GoalMaker uses, so there is no password to store and no browser redirect for a
desktop app to catch. Rows carry `user_id`, the primary key is `(user_id, id)` because the first-run
sample rows share fixed IDs across installs, and row security limits every operation to the owner.
Sessions live only in OS-protected storage.

Migration `0002_per_user_rows.sql` cannot know who owned the anonymous rows. Rather than delete
them or let the first user to sign in claim them, it moves them to `subscriptions_legacy_0002`,
which no API role can read. Every device holds a complete local copy and uploads it after signing
in, so nothing is lost, and the owner drops the legacy table once the devices agree. Old app
versions stop syncing the moment the migration runs; they keep working offline.

The apps talk to the REST endpoints directly instead of using the Supabase SDKs. Sync already used
plain HTTP for PostgREST, the auth calls are four requests, and both apps stay free of a large
dependency whose update cadence would then drive theirs.

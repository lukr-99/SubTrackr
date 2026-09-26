# Sync setup

Sync is optional. SubTrackr works fully offline, and nothing in the apps points at a server until
you enter one. To sync, you bring your own Supabase project and sign in with an email code on each
device. Only subscriptions sync; base currency, budget, theme, and the other settings stay on each
device. The merge is last-writer-wins per subscription ([SPEC section 8](../contracts/SPEC.md#8-sync)).

## 1. Create a project

Sign up at <https://supabase.com>, create a project, and wait for it to finish provisioning. The
free tier is plenty.

## 2. Create the table

Open **SQL Editor**, then paste and run each file from
[`supabase/migrations/`](../supabase/migrations/) in order: `0001_init.sql`, then
`0002_per_user_rows.sql`. Both are safe to run twice.

If you manage the project with the Supabase CLI instead, `npx supabase link` and
`npx supabase db push` apply the same files. When you already pasted them by hand, first mark them
applied with `npx supabase migration repair --status applied 0001 0002`.

## 3. Send sign-in codes

SubTrackr signs in with a 6-digit code, not a link. Under **Authentication, Emails**, replace the
body of both the **Magic link** and the **Confirm signup** templates with the contents of
[`supabase/templates/sign-in-code.html`](../supabase/templates/sign-in-code.html), and set the
subject to "Your SubTrackr sign-in code". The template shows `{{ .Token }}`, which is the code.

Supabase's built-in mail sender only allows a few emails per hour. That is enough for a couple of
devices; configure your own SMTP server under **Authentication, Emails** if you need more.

## 4. Find the URL and key

Under **Project Settings, API Keys** you need the project URL (`https://<ref>.supabase.co`) and the
**publishable** key (`sb_publishable_...`). The publishable key only identifies the project; row
security keeps every user to their own rows. Never paste the secret key or the `service_role` key
into an app.

## 5. Connect each device

In the app, open **Settings, Sync**:

1. Paste the project URL and the publishable key.
2. Enter your email and choose **Send code**.
3. Type the code from the email and choose **Verify**.

The app syncs right away and then on its own after every change. **Sync now** runs a pass by hand,
and the status line shows the last result. Use the same email on every device.

## Upgrading from 0.2.x

Versions up to 0.2.2 synced through Supabase's anonymous role. To move an existing project over:

1. Run `0002_per_user_rows.sql` as in step 2. It moves the old rows into
   `subscriptions_legacy_0002`, which no app can read, and old app versions stop syncing.
2. Set up the email templates from step 3.
3. Install 0.3.0 or later on every device and sign in on each one. Each device uploads its full
   local copy, and the merge brings them together.
4. When every device shows the same list, drop the old rows:
   `drop table public.subscriptions_legacy_0002;`

## Running Supabase locally

For development, the repository pins the Supabase CLI and has a local configuration. Docker must be
running.

```powershell
npm ci
npx supabase start -x studio,imgproxy,vector,logflare,supavisor,storage-api,realtime,edge-runtime,postgres-meta
npx supabase status
```

The API listens on `http://127.0.0.1:54621` (`http://10.0.2.2:54621` from the Android emulator;
only debug builds allow plain HTTP there), and sign-in codes land in Mailpit at
`http://127.0.0.1:54624`. `npx supabase status` prints the local publishable key. Stop the stack
with `npx supabase stop`.

`python tools/supabase_migrations.py test` checks the whole migration chain against the running
stack: the full chain with the pgTAP row-security tests, each migration applied in isolation on
top of fixture data, and each applied twice. It resets the local database.

## Removing your data

Signing out keeps your local data. To delete what the server holds, delete your user under
**Authentication, Users**; your rows go with it. To remove everything, delete the project.

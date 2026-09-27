# SubTrackr context

The words SubTrackr's code, docs, and UI use for its own ideas. Use the term in the left column;
avoid the ones in the last.

| Term | Meaning | Avoid |
| --- | --- | --- |
| Subscription | One recurring payment the user tracks, with a stable UUID. | Service, plan, item |
| Billing cycle | How often a subscription charges: weekly, monthly, quarterly, semiannual, annual, or a custom number of days. | Period, frequency |
| Monthly equivalent | A subscription's cost scaled to one average month in its own currency (SPEC section 2). | Monthly cost, normalized price |
| Base currency | The currency every total is converted into. Stored per device. | Main currency, home currency |
| Active spend | The monthly or yearly total over active, non-deleted subscriptions in the base currency. | Total, burn |
| Cost per use | Monthly equivalent in the base currency divided by uses per month. | Price per use |
| Worth threshold | The cost per use at or below which an automatic verdict is "worth it". Stored per device. | Limit, cutoff |
| Worth mode | The user's choice for one subscription: Auto, Essential, Always worth, Not worth. | Worth setting, override flag |
| Worth verdict | The badge that results: Essential, Worth, Not worth, or Unknown. The mode decides; Auto computes it. | Worth status |
| Paused | Still listed and synced, but left out of active spend. | Inactive, disabled |
| Tombstone | A subscription with `deleted_at` set. Hidden, but kept so the deletion reaches other devices. | Soft-deleted row, trash |
| Sample subscriptions | The generic first-run set with fixed IDs from `contracts/vectors/seed-data.json`. | Seed rows (in UI text), demo data |
| Sync | Pull, merge, save, push of subscriptions between a device and the user's own Supabase project. | Backup, upload |
| Merge | Last writer wins per subscription by `updated_at`; a tombstone wins a tie, then the remote side. | Conflict resolution |
| Project | The user's Supabase project: its URL and publishable key. Stored per device, never synced or backed up. | Server, account |
| Backup | A versioned JSON file with every subscription and the settings except the project. | Export, dump |
| Restore | Reading a backup back in, by Merge (the default) or Replace. | Import |
| Dev build | Any build that is not a release: version `X.Y.Z-dev`, its own data, no update checks. | Debug version, test build |
| Logo | The SubTrackr mark from `contracts/design/logo.json`. | Icon (unless you mean a platform icon file) |
| Service logo | The favicon shown for a subscription's website. | Logo (on its own), brand icon |

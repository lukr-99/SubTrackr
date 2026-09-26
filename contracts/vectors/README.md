# Golden vectors

These JSON files pin behavior the two apps must agree on. Each app has tests that load every file
here and check its own implementation against it, so drift on either side turns that side's suite
red.

## Rules

- Vectors change first. When behavior in [`../SPEC.md`](../SPEC.md) changes, update the vector,
  watch both suites fail, then fix both apps.
- Money and rates are decimal strings, never JSON floats.
- Each file names the spec section it verifies.
- Test data is generic. Never put real subscriptions, keys, or project URLs here.

## Files

| File | Verifies |
| --- | --- |
| `monthly-normalization.json` | Section 2, billing cycle to monthly equivalent |
| `currency-conversion.json` | Section 3, conversion and rounding |
| `worth-it.json` | Section 5, worth verdicts and overrides |
| `merge.json` | Section 8.1, last-writer-wins merge |
| `sync-rows.json` | Section 8.3, subscription to Supabase row mapping |
| `backup.json` | Section 9, backup validation and restore |
| `release-selection.json` | Section 11, update discovery, asset choice, checksum files |
| `seed-data.json` | Fixed IDs of the first-run sample subscriptions |

The theme palette in [`../design/tokens.json`](../design/tokens.json) and the logo in
[`../design/logo.json`](../design/logo.json) work the same way: both apps
test against them.

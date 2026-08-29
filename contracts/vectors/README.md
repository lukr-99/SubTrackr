# Golden vectors — the behavior contract

These JSON files pin **behavior** the two apps must agree on. Each app ships a
test that loads every vector file here and asserts its own implementation
matches. If either app drifts from the spec, its own test suite goes red.

## Rules

- **Vectors change first.** When behavior in [`../SPEC.md`](../SPEC.md) changes,
  update the vector, watch both suites fail, then fix both apps.
- Numbers are **strings** (decimal), never JSON floats, to avoid parser drift.
- Each file names the `spec` section it verifies and any `comparePrecision`
  (decimal places to round to before comparing).

## Files

| File | Verifies |
|---|---|
| `monthly-normalization.json` | SPEC §2 — billing cycle → monthly-equivalent |
| `seed-data.json` | Stable cross-platform IDs for first-run subscriptions |

_(more added as we implement conversion, totals, and the worth-it verdict)_

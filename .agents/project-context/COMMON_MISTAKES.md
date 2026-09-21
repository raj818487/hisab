---
version: 1.0.0
last-reviewed: 2026-09-21
---

# Common Mistakes

- Inline templates / styles in `.page.ts` (forbidden after refactor)
- Calling `HttpClient` from a page (use feature service)
- Adding payment gateway SDKs
- Regenerating receipt numbers for existing payments
- Skipping unique `(CustomerId, Date)` checks on daily upsert

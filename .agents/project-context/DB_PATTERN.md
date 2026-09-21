---
version: 1.0.0
last-reviewed: 2026-09-21
---

# DB Pattern

- Provider: SQLite (`ConnectionStrings:Default`)
- Tables: Customers, DailyEntries, Payments
- Unique: `(CustomerId, Date)` on daily; `ReceiptNumber` on payments
- Precision: money decimal(12,2); litres decimal(10,3)
- Seed: `Data/DbSeeder.cs` after `EnsureCreated`

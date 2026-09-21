# FULL-GAP-FILL — Critical files for Milk Hisab DoD

P5 assumes P1–P4 already landed the core app on Windows. This list is the **minimum set that must exist** for launch DoD. Items marked **(P5)** are delivered by this pack; others must already be present (or restored from p1-pack).

## Backend (MilkHisab.Api)

| File | Why critical | Source |
|------|--------------|--------|
| `Models/Customer.cs` | Customer entity | P1 |
| `Models/DailyEntry.cs` | Taken/skip daily rows | P1 |
| `Models/Payment.cs` | Manual payment + ReceiptNumber | P1 |
| `Data/AppDbContext.cs` | EF + unique receipt / day indexes | P1 |
| `Data/DbSeeder.cs` | Demo data for showcase | **P5** |
| `Program.cs` | DI, CORS, EnsureCreated/Migrate + **seeder call** | P1 + **P5 patch** |
| `Services/CustomerService.cs` (+ interface) | CRUD | P1 |
| `Services/DailyEntryService.cs` (+ interface) | Day-board upsert | P1 |
| `Services/HisabService.cs` (+ interface) | Due = delivered − paid | **P5** (refresh) |
| `Services/PaymentService.cs` (+ interface) | Manual ledger | **P5** (uses receipt svc) |
| `Services/ReceiptService.cs` (+ interface) | Stable numbers + HTML | **P5** |
| `Controllers/CustomersController.cs` | `/api/v1/customers` | P1 |
| `Controllers/DailyEntriesController.cs` | Day-board | P1 |
| `Controllers/HisabController.cs` | Month hisab | P1 |
| `Controllers/PaymentsController.cs` | Record payment | P1 |
| `Controllers/ReceiptsController.cs` | Receipt JSON + HTML | P1 |
| `Controllers/HealthController.cs` | Smoke health | **P5** |
| `appsettings.json` | SQLite connection string | P1 |
| `Properties/launchSettings.json` | `http://localhost:8080` | P1 |
| `MilkHisab.Api.csproj` | EF Sqlite packages | P1 |

## Frontend (Angular standalone PWA)

| File | Why critical | Source |
|------|--------------|--------|
| `app.config.ts` | **`provideHttpClient()`** required | **P5** |
| `app.routes.ts` | Routes incl. `/receipts/:paymentId` | P1 |
| `core/api.service.ts` | All API calls | P1 |
| `environments/environment*.ts` | `apiBaseUrl` → `:8080/api/v1` | P1 |
| `features/dashboard/dashboard.page.ts` | Today counts + month due | **P5** |
| `features/daily/daily.page.ts` | Take/skip board | **P5** |
| `features/customers/customers-list.page.ts` | Empty state | **P5** |
| `features/customers/customer-form.page.ts` | Create/edit | P1 |
| `features/hisab/hisab.page.ts` | Due formula visible | **P5** |
| `features/payments/payments.page.ts` | Manual record → receipt | **P5** |
| `features/receipts/receipt.page.ts` | **Download + Print** | **P5** |
| `manifest.webmanifest` (+ icons) | PWA install | Must exist from earlier day / ng add pwa |
| Service worker config | Offline shell (optional for MVP) | Prefer present |

## Docs / scripts

| File | Why critical | Source |
|------|--------------|--------|
| `docs/LAUNCH-CHECKLIST.md` | DoD walkthrough | **P5** |
| `docs/RUNBOOK.md` | Run API+UI | **P5** |
| `scripts/smoke-api.ps1` | Automated smoke | **P5** |
| `APPLY.ps1` | Idempotent copy + optional Program patch | **P5** |

## Gaps to watch (not in P5 pack — fix on Windows if missing)

1. **PWA manifest / icons** — if `ng add @angular/pwa` was never run, install before claiming PWA DoD.
2. **EF migrations** — pack uses `EnsureCreated`; if project switched to migrations, call `Migrate()` then seeder (snippet covers both).
3. **JSON camelCase** — ensure API serializes camelCase so Angular DTOs bind (`AddControllers` defaults in recent templates are fine; verify if customized).
4. **Circular DI** — `PaymentService` → `IReceiptService` → `IHisabService` only; do not inject `IPaymentService` into ReceiptService.
5. **Auth** — plan mentioned PIN/auth stretch; not required for P5 MVP manual-payments DoD.
6. **Export/share monthly statement** — listed in plan DoD; if missing from P1–P4, treat as stretch or add a simple hisab table copy — not blocked by payment-gateway rule.

## Forbidden (must remain absent)

- Razorpay / Stripe / Paytm / PhonePe SDKs
- Payment capture / webhook / order_id gateway flows
- Any “Pay now” online collection UI

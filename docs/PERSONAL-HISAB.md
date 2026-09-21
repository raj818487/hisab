# Personal and product hisab

The default `/personal` page supports a person keeping their own records without a vendor account. It uses the existing purple/navy theme, responsive summary cards, category bars, product filters and an outstanding-purchases list.

## Use

1. Create a username/password account (password: at least 10 characters).
2. Add an expense directly with an amount and category. No customer is required.
3. Optionally add a product, then record quantity and rate. Each entry stores its rate; later product defaults do not rewrite history.
4. Choose paid in full, partially paid, or pay later. Payments settle a specific purchase and never create a second expense.
5. Filter by month, category or product. Opening and closing balances include previous months. The outstanding panel shows current unpaid purchases across all months.
6. Record later payments and view/download/print their HTML records.

The existing milk business routes remain available. Customer DELETE now archives rather than deleting history, and archived customers with activity remain in business reports.

## Data and access

Personal users, products, expenses and payments are new tables. `PersonalSchema.Upgrade` runs an additive, transactional, repeatable SQLite upgrade; existing milk data is not assigned to a new personal account or rewritten. New databases have matching EF foreign keys. Personal endpoints require cookie authentication and scope every read/write to the signed-in owner. Passwords use the ASP.NET Core password hasher, cookies are HTTP-only with SameSite Strict, and login/registration are rate limited.

The legacy business API still uses the original shared/local model, not account-scoped business ownership. Do not expose that legacy API as a multi-tenant service. This change does not implement business team accounts, automatic conversion of deliveries to personal purchases, password recovery, income/budget tracking, editing/deleting posted personal transactions, or offline write synchronization. No gateway or new package was added.

## Run

- Backend: from `backend`, `dotnet run --urls http://localhost:8080`.
- Frontend: from `frontend`, `npm start`.
- Open `http://localhost:4200/personal`.
- Cookie keys default to a `keys` directory beside the API binary. For deployment, set `DataProtection__KeyPath` to persistent protected storage, back it up with the database, and use HTTPS. Development CORS permits `http://localhost:4200`.

## Verification

From the repository root:

```powershell
dotnet build backend --no-restore -o .verification/backend
node scripts/check-personal.mjs
npm --prefix frontend run build
$env:CHROME_BIN = 'C:/Program Files/Google/Chrome/Application/chrome.exe'
npm --prefix frontend test -- --watch=false --browsers=ChromeHeadless
```

The API check uses a temporary database and disposable accounts. It verifies authentication, isolation, server-calculated money, partial settlement, invalid dates/amounts, and no duplicate expense on payment. The Angular checks exercise carry-forward, product-specific settlement, rendered signed-in content using fixtures, and successful saves followed by a failed refresh.

Read-only browser verification covers the real sign-in page at desktop and 390px mobile widths. Creating a persistent test account through the browser was blocked by automatic approval review; no such account was created.

Known build warnings: existing SQLitePCLRaw native dependency advisory NU1903; personal page CSS exceeds the 4 kB warning budget but is below the 8 kB error budget.

# Feature — Daily entries

## Purpose
Per-day board: mark take (qty+rate) or skip (qty 0) per active customer.

## Frontend
- `features/daily/daily.page.{ts,html,scss}`
- `features/daily/daily.service.ts`

## Backend
- `Controllers/DailyEntriesController.cs`
- `Services/IDailyEntryService.cs`, `DailyEntryService.cs`
- Unique `(CustomerId, Date)`

## API
- `GET /api/v1/daily-entries/day-board?date=yyyy-MM-dd`
- `POST /api/v1/daily-entries` upsert

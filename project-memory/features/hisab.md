# Feature — Monthly hisab

## Purpose
Month summary: delivered, paid, **due = delivered − paid** per customer.

## Frontend
- `features/hisab/hisab.page.{ts,html,scss}`
- `features/hisab/hisab.service.ts`

## Backend
- `Controllers/HisabController.cs`
- `Services/IHisabService.cs`, `HisabService.cs`

## Formula
`DueAmount = DeliveredAmount - PaidAmount` (manual payments only).

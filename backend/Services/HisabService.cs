using Microsoft.EntityFrameworkCore;
using MilkHisab.Api.Data;
using MilkHisab.Api.DTOs;

namespace MilkHisab.Api.Services;

/// <summary>
/// Month hisab. Contract: DueAmount = DeliveredAmount − PaidAmount
/// (delivered from daily taken litres×rate; paid from manual payment ledger).
/// </summary>
public class HisabService : IHisabService
{
    private readonly AppDbContext _db;

    public HisabService(AppDbContext db) => _db = db;

    public async Task<HisabMonthSummaryDto> GetMonthSummaryAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var customers = await _db.Customers.AsNoTracking()
            .Where(c => c.IsActive || c.DailyEntries.Any() || c.Payments.Any())
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var deliveries = await _db.DailyEntries.AsNoTracking()
            .Where(e => e.Date >= start && e.Date < end)
            .GroupBy(e => e.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                // Skip days (qty 0) contribute 0; taken days sum litres×rate
                Amount = g.Sum(x => x.QuantityLitres * x.Rate),
                Litres = g.Sum(x => x.QuantityLitres)
            })
            .ToDictionaryAsync(x => x.CustomerId, ct);

        var payments = await _db.Payments.AsNoTracking()
            .Where(p => p.PaidOn >= start && p.PaidOn < end)
            .GroupBy(p => p.CustomerId)
            .Select(g => new { CustomerId = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.CustomerId, ct);

        var rows = customers.Select(c =>
        {
            deliveries.TryGetValue(c.Id, out var d);
            payments.TryGetValue(c.Id, out var p);
            var delivered = d?.Amount ?? 0m;
            var paid = p?.Amount ?? 0m;
            // DoD formula: due = delivered − paid
            var due = delivered - paid;
            return new CustomerHisabDto(
                c.Id, c.Name, delivered, paid, due, d?.Litres ?? 0m);
        }).ToList();

        return new HisabMonthSummaryDto(
            year, month,
            rows.Sum(r => r.DeliveredAmount),
            rows.Sum(r => r.PaidAmount),
            rows.Sum(r => r.DueAmount),
            rows);
    }

    public async Task<CustomerHisabDto?> GetCustomerMonthAsync(
        int customerId, int year, int month, CancellationToken ct = default)
    {
        var summary = await GetMonthSummaryAsync(year, month, ct);
        return summary.Customers.FirstOrDefault(c => c.CustomerId == customerId);
    }
}


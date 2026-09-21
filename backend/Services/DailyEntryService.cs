using Microsoft.EntityFrameworkCore;
using MilkHisab.Api.Data;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Services;

public class DailyEntryService : IDailyEntryService
{
    private readonly AppDbContext _db;

    public DailyEntryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<DayBoardItemDto>> GetDayBoardAsync(DateOnly date, CancellationToken ct = default)
    {
        var customers = await _db.Customers.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var entries = await _db.DailyEntries.AsNoTracking()
            .Where(e => e.Date == date)
            .ToDictionaryAsync(e => e.CustomerId, ct);

        return customers.Select(c =>
        {
            entries.TryGetValue(c.Id, out var e);
            return new DayBoardItemDto(
                c.Id,
                c.Name,
                c.DefaultRate,
                e?.Id,
                e?.QuantityLitres,
                e?.Rate,
                e is null ? null : e.QuantityLitres * e.Rate,
                e?.Note);
        }).ToList();
    }

    public async Task<IReadOnlyList<DailyEntryDto>> GetByCustomerAsync(
        int customerId, int? year = null, int? month = null, CancellationToken ct = default)
    {
        var q = _db.DailyEntries.AsNoTracking()
            .Include(e => e.Customer)
            .Where(e => e.CustomerId == customerId);

        if (year is int y)
            q = q.Where(e => e.Date.Year == y);
        if (month is int m)
            q = q.Where(e => e.Date.Month == m);

        return await q.OrderByDescending(e => e.Date)
            .Select(e => new DailyEntryDto(
                e.Id, e.CustomerId, e.Customer!.Name, e.Date,
                e.QuantityLitres, e.Rate, e.QuantityLitres * e.Rate, e.Note))
            .ToListAsync(ct);
    }

    public async Task<DailyEntryDto> UpsertAsync(UpsertDailyEntryRequest request, CancellationToken ct = default)
    {
        var existing = await _db.DailyEntries
            .FirstOrDefaultAsync(e => e.CustomerId == request.CustomerId && e.Date == request.Date, ct);

        if (existing is null)
        {
            existing = new DailyEntry
            {
                CustomerId = request.CustomerId,
                Date = request.Date,
                CreatedAt = DateTime.UtcNow
            };
            _db.DailyEntries.Add(existing);
        }

        existing.QuantityLitres = request.QuantityLitres;
        existing.Rate = request.Rate;
        existing.Note = request.Note;
        await _db.SaveChangesAsync(ct);

        var name = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == request.CustomerId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        return new DailyEntryDto(
            existing.Id, existing.CustomerId, name, existing.Date,
            existing.QuantityLitres, existing.Rate,
            existing.QuantityLitres * existing.Rate, existing.Note);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.DailyEntries.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity is null) return false;
        _db.DailyEntries.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

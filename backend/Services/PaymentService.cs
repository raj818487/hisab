using Microsoft.EntityFrameworkCore;
using MilkHisab.Api.Data;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Services;

/// <summary>
/// Manual payment ledger. Does NOT integrate with Razorpay, UPI apps, or any gateway.
/// Mode is stored as a simple label (cash / upi / other).
/// Receipt numbers come from <see cref="IReceiptService.NextReceiptNumberAsync"/>.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IReceiptService _receipts;

    public PaymentService(AppDbContext db, IReceiptService receipts)
    {
        _db = db;
        _receipts = receipts;
    }

    public async Task<PaymentDto> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        var mode = NormalizeMode(request.Mode);
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be positive.");

        var customerExists = await _db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
            throw new InvalidOperationException($"Customer {request.CustomerId} not found.");

        var receiptNumber = await _receipts.NextReceiptNumberAsync(ct);

        var entity = new Payment
        {
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Mode = mode,
            PaidOn = request.PaidOn,
            Note = request.Note?.Trim(),
            ReceiptNumber = receiptNumber,
            CreatedAt = DateTime.UtcNow
        };

        _db.Payments.Add(entity);
        await _db.SaveChangesAsync(ct);

        var name = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == request.CustomerId)
            .Select(c => c.Name)
            .FirstAsync(ct);

        return ToDto(entity, name);
    }

    public async Task<PaymentDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var p = await _db.Payments.AsNoTracking()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return p is null ? null : ToDto(p, p.Customer?.Name);
    }

    public async Task<IReadOnlyList<PaymentDto>> ListByCustomerAsync(
        int customerId, int? year = null, int? month = null, CancellationToken ct = default)
    {
        var q = _db.Payments.AsNoTracking()
            .Include(p => p.Customer)
            .Where(p => p.CustomerId == customerId);

        if (year is int y) q = q.Where(p => p.PaidOn.Year == y);
        if (month is int m) q = q.Where(p => p.PaidOn.Month == m);

        var list = await q.OrderByDescending(p => p.PaidOn).ThenByDescending(p => p.Id)
            .ToListAsync(ct);
        return list.Select(p => ToDto(p, p.Customer?.Name)).ToList();
    }

    public async Task<IReadOnlyList<PaymentDto>> ListByMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var list = await _db.Payments.AsNoTracking()
            .Include(p => p.Customer)
            .Where(p => p.PaidOn >= start && p.PaidOn < end)
            .OrderByDescending(p => p.PaidOn)
            .ToListAsync(ct);
        return list.Select(p => ToDto(p, p.Customer?.Name)).ToList();
    }

    private static string NormalizeMode(string? mode)
    {
        var m = (mode ?? "cash").Trim().ToLowerInvariant();
        return m switch
        {
            "upi" => "upi",
            "other" => "other",
            _ => "cash"
        };
    }

    private static PaymentDto ToDto(Payment p, string? name) =>
        new(p.Id, p.CustomerId, name, p.Amount, p.Mode, p.PaidOn, p.Note, p.ReceiptNumber, p.CreatedAt);
}

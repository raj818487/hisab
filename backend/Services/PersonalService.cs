using Microsoft.EntityFrameworkCore;
using MilkHisab.Api.Data;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Services;

public class PersonalService(AppDbContext db) : IPersonalService
{
    public async Task<object> GetAsync(int ownerId) => new
    {
        Products = await db.LedgerProducts.AsNoTracking().Where(x => x.OwnerId == ownerId).OrderBy(x => x.Name).ToListAsync(),
        Expenses = await db.Expenses.AsNoTracking().Where(x => x.OwnerId == ownerId).OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToListAsync(),
        Payments = await db.ExpensePayments.AsNoTracking().Where(x => x.OwnerId == ownerId).OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToListAsync()
    };

    public async Task<LedgerProduct> AddProductAsync(int ownerId, ProductRequest request)
    {
        var name = Require(request.Name, 120);
        var unit = Require(request.Unit, 24);
        Money(request.Rate, allowZero: true);
        var row = new LedgerProduct { OwnerId = ownerId, Name = name, Unit = unit, Rate = request.Rate };
        db.LedgerProducts.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    public async Task DeleteProductAsync(int ownerId, int id)
    {
        var row = await db.LedgerProducts.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId)
            ?? throw new ArgumentException("Product not found.");
        if (await db.Expenses.AnyAsync(x => x.OwnerId == ownerId && x.ProductId == id))
            throw new ArgumentException("This product is used on expenses. Remove or edit those expenses first.");
        db.LedgerProducts.Remove(row);
        await db.SaveChangesAsync();
    }

    public Task<Expense> AddExpenseAsync(int ownerId, ExpenseRequest request) =>
        SaveExpenseAsync(ownerId, null, request);

    public Task<Expense> UpdateExpenseAsync(int ownerId, int id, ExpenseRequest request) =>
        SaveExpenseAsync(ownerId, id, request);

    async Task<Expense> SaveExpenseAsync(int ownerId, int? id, ExpenseRequest request)
    {
        Day(request.Date);
        var title = Require(request.Title, 120);
        var category = Require(request.Category, 60);
        if (request.Supplier is { Length: > 120 }) throw new ArgumentException("Supplier is too long.");
        if (request.Note is { Length: > 1000 }) throw new ArgumentException("Note is too long.");

        decimal amount = request.Amount;
        decimal? quantity = null;
        decimal? rate = null;
        int? productId = request.ProductId;

        if (productId is int pid)
        {
            if (!await db.LedgerProducts.AnyAsync(x => x.Id == pid && x.OwnerId == ownerId))
                throw new ArgumentException("Product not found.");
            if (request.Quantity is not decimal qty || qty <= 0 || qty > 100_000m || decimal.Round(qty, 3) != qty)
                throw new ArgumentException("Enter a valid quantity (up to 3 decimals).");
            if (request.Rate is not decimal rt) throw new ArgumentException("Enter a rate.");
            Money(rt, allowZero: true);
            quantity = qty;
            rate = rt;
            amount = decimal.Round(qty * rt, 2, MidpointRounding.AwayFromZero);
        }
        Money(amount);
        Money(request.Paid, allowZero: true);
        if (request.Paid > amount) throw new ArgumentException("Paid amount cannot exceed the expense.");
        var mode = Mode(request.Mode);

        await using var tx = await db.Database.BeginTransactionAsync();
        Expense row;
        if (id is int expenseId)
        {
            row = await db.Expenses.SingleOrDefaultAsync(x => x.Id == expenseId && x.OwnerId == ownerId)
                ?? throw new ArgumentException("Expense not found.");
            var paidSoFar = await db.ExpensePayments.Where(x => x.ExpenseId == expenseId && x.OwnerId == ownerId).SumAsync(x => x.Amount);
            if (paidSoFar > amount)
                throw new ArgumentException("New amount is lower than payments already recorded. Adjust payments first.");
            row.Date = request.Date;
            row.Title = title;
            row.Category = category;
            row.Supplier = string.IsNullOrWhiteSpace(request.Supplier) ? null : request.Supplier.Trim();
            row.ProductId = productId;
            row.Quantity = quantity;
            row.Rate = rate;
            row.Amount = amount;
            row.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            await db.SaveChangesAsync();
        }
        else
        {
            row = new Expense
            {
                OwnerId = ownerId,
                Date = request.Date,
                Title = title,
                Category = category,
                Supplier = string.IsNullOrWhiteSpace(request.Supplier) ? null : request.Supplier.Trim(),
                ProductId = productId,
                Quantity = quantity,
                Rate = rate,
                Amount = amount,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim()
            };
            db.Expenses.Add(row);
            await db.SaveChangesAsync();
            if (request.Paid > 0)
            {
                db.ExpensePayments.Add(new ExpensePayment
                {
                    OwnerId = ownerId,
                    ExpenseId = row.Id,
                    Date = request.Date,
                    Amount = request.Paid,
                    Mode = mode
                });
                await db.SaveChangesAsync();
            }
        }
        await tx.CommitAsync();
        return row;
    }

    public async Task DeleteExpenseAsync(int ownerId, int id)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var row = await db.Expenses.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId)
            ?? throw new ArgumentException("Expense not found.");
        var payments = await db.ExpensePayments.Where(x => x.ExpenseId == id && x.OwnerId == ownerId).ToListAsync();
        db.ExpensePayments.RemoveRange(payments);
        db.Expenses.Remove(row);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task<ExpensePayment> PayAsync(int ownerId, int id, ExpensePaymentRequest request)
    {
        Day(request.Date);
        Money(request.Amount);
        var mode = Mode(request.Mode);
        await using var tx = await db.Database.BeginTransactionAsync();
        var expense = await db.Expenses.SingleOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId)
            ?? throw new ArgumentException("Expense not found.");
        if (request.Date < expense.Date) throw new ArgumentException("Payment date cannot be before the expense date.");
        var paid = await db.ExpensePayments.Where(x => x.ExpenseId == id && x.OwnerId == ownerId).SumAsync(x => x.Amount);
        var due = expense.Amount - paid;
        if (request.Amount > due) throw new ArgumentException("Payment is more than the remaining due.");
        var payment = new ExpensePayment
        {
            OwnerId = ownerId,
            ExpenseId = id,
            Date = request.Date,
            Amount = request.Amount,
            Mode = mode
        };
        db.ExpensePayments.Add(payment);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return payment;
    }

    static string Require(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new ArgumentException($"Enter between 1 and {max} characters.");
        return value.Trim();
    }

    static void Money(decimal value, bool allowZero = false)
    {
        if (value < 0 || (!allowZero && value == 0) || value > 100_000_000m || decimal.Round(value, 2) != value)
            throw new ArgumentException("Enter a valid amount (max 2 decimals).");
    }

    static void Day(DateOnly date)
    {
        if (date.Year < 2000 || date > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Pick a date from year 2000 through today.");
    }

    static string Mode(string? mode) => mode is "cash" or "upi" or "other"
        ? mode
        : throw new ArgumentException("Choose cash, upi, or other.");
}

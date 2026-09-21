using Microsoft.EntityFrameworkCore;
using MilkHisab.Api.Data;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(bool activeOnly = false, CancellationToken ct = default)
    {
        var q = _db.Customers.AsNoTracking();
        if (activeOnly) q = q.Where(c => c.IsActive);
        var list = await q.OrderBy(c => c.Name).ToListAsync(ct);
        return list.Select(ToDto).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? null : ToDto(c);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var entity = new Customer
        {
            Name = request.Name.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            DefaultRate = request.DefaultRate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.Customers.Add(entity);
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;

        entity.Name = request.Name.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Address = request.Address?.Trim();
        entity.DefaultRate = request.DefaultRate;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return false;
        entity.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static CustomerDto ToDto(Customer c) =>
        new(c.Id, c.Name, c.Phone, c.Address, c.DefaultRate, c.IsActive, c.CreatedAt);
}


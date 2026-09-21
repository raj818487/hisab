using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MilkHisab.Api.Data;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Services;

public class AccountService(AppDbContext db) : IAccountService
{
    public async Task<LedgerUser?> AuthenticateAsync(Credentials r, bool register)
    {
        var name = r.Name?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(name) || name.Length > 80 || string.IsNullOrEmpty(r.Password) || r.Password.Length < 10 || r.Password.Length > 200)
            throw new ArgumentException("Enter a username (up to 80 characters) and a password of 10-200 characters.");
        var user = await db.LedgerUsers.SingleOrDefaultAsync(x => x.Name == name);
        var hasher = new PasswordHasher<LedgerUser>();
        if (register)
        {
            if (user != null) throw new InvalidOperationException("This username is already registered.");
            user = new LedgerUser { Name = name };
            user.PasswordHash = hasher.HashPassword(user, r.Password);
            db.LedgerUsers.Add(user);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException)
            { throw new InvalidOperationException("This username is already registered."); }
        }
        else if (user == null || hasher.VerifyHashedPassword(user, user.PasswordHash, r.Password) == PasswordVerificationResult.Failed)
            return null;
        return user;
    }

    public Task<LedgerUser?> FindByNameAsync(string name)
    {
        var n = name?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(n)) return Task.FromResult<LedgerUser?>(null);
        return db.LedgerUsers.SingleOrDefaultAsync(x => x.Name == n);
    }
}
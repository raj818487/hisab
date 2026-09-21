using MilkHisab.Api.DTOs;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Services;

public interface IAccountService
{
    Task<LedgerUser?> AuthenticateAsync(Credentials r, bool register);
    Task<LedgerUser?> FindByNameAsync(string name);
}
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Models;

namespace MilkHisab.Api.Services;

public interface IPersonalService
{
    Task<object> GetAsync(int ownerId);
    Task<LedgerProduct> AddProductAsync(int ownerId, ProductRequest request);
    Task<Expense> AddExpenseAsync(int ownerId, ExpenseRequest request);
    Task<Expense> UpdateExpenseAsync(int ownerId, int id, ExpenseRequest request);
    Task DeleteExpenseAsync(int ownerId, int id);
    Task DeleteProductAsync(int ownerId, int id);
    Task<ExpensePayment> PayAsync(int ownerId, int id, ExpensePaymentRequest request);
}

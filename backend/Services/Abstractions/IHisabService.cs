using MilkHisab.Api.DTOs;

namespace MilkHisab.Api.Services;

public interface IHisabService
{
    Task<HisabMonthSummaryDto> GetMonthSummaryAsync(int year, int month, CancellationToken ct = default);
    Task<CustomerHisabDto?> GetCustomerMonthAsync(int customerId, int year, int month, CancellationToken ct = default);
}

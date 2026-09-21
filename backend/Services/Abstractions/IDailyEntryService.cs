using MilkHisab.Api.DTOs;

namespace MilkHisab.Api.Services;

public interface IDailyEntryService
{
    Task<IReadOnlyList<DayBoardItemDto>> GetDayBoardAsync(DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<DailyEntryDto>> GetByCustomerAsync(int customerId, int? year = null, int? month = null, CancellationToken ct = default);
    Task<DailyEntryDto> UpsertAsync(UpsertDailyEntryRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

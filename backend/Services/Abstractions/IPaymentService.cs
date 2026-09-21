using MilkHisab.Api.DTOs;

namespace MilkHisab.Api.Services;

public interface IPaymentService
{
    Task<PaymentDto> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default);
    Task<PaymentDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentDto>> ListByCustomerAsync(int customerId, int? year = null, int? month = null, CancellationToken ct = default);
    Task<IReadOnlyList<PaymentDto>> ListByMonthAsync(int year, int month, CancellationToken ct = default);
}

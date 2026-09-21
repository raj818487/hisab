using MilkHisab.Api.DTOs;

namespace MilkHisab.Api.Services;

public interface IReceiptService
{
    /// <summary>
    /// Stable sequential receipt number: MH-yyyyMMdd-#### (UTC day prefix).
    /// Generated at payment record time; never regenerated for existing payments.
    /// </summary>
    Task<string> NextReceiptNumberAsync(CancellationToken ct = default);

    Task<ReceiptDto?> GetReceiptAsync(int paymentId, CancellationToken ct = default);
    Task<string?> GetReceiptHtmlAsync(int paymentId, CancellationToken ct = default);
}

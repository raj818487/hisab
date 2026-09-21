namespace MilkHisab.Api.DTOs;

/// <summary>
/// Manual payment record. Mode is cash/upi/other label — no gateway charge.
/// </summary>
public record PaymentDto(
    int Id,
    int CustomerId,
    string? CustomerName,
    decimal Amount,
    string Mode,
    DateOnly PaidOn,
    string? Note,
    string ReceiptNumber,
    DateTime CreatedAt);

public record RecordPaymentRequest(
    int CustomerId,
    decimal Amount,
    string Mode,
    DateOnly PaidOn,
    string? Note);

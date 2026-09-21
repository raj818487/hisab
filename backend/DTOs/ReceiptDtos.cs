namespace MilkHisab.Api.DTOs;

public record ReceiptDto(
    string ReceiptNumber,
    int PaymentId,
    int CustomerId,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerAddress,
    decimal Amount,
    string Mode,
    DateOnly PaidOn,
    string? Note,
    DateTime GeneratedAt,
    decimal MonthDelivered,
    decimal MonthPaid,
    decimal MonthDue,
    int Year,
    int Month);

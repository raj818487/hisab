namespace MilkHisab.Api.DTOs;

public record HisabMonthSummaryDto(
    int Year,
    int Month,
    decimal TotalDelivered,
    decimal TotalPaid,
    decimal TotalDue,
    IReadOnlyList<CustomerHisabDto> Customers);

public record CustomerHisabDto(
    int CustomerId,
    string CustomerName,
    decimal DeliveredAmount,
    decimal PaidAmount,
    decimal DueAmount,
    decimal TotalLitres);

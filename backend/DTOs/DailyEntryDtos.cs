namespace MilkHisab.Api.DTOs;

public record DailyEntryDto(
    int Id,
    int CustomerId,
    string? CustomerName,
    DateOnly Date,
    decimal QuantityLitres,
    decimal Rate,
    decimal Amount,
    string? Note);

public record UpsertDailyEntryRequest(
    int CustomerId,
    DateOnly Date,
    decimal QuantityLitres,
    decimal Rate,
    string? Note);

public record DayBoardItemDto(
    int CustomerId,
    string CustomerName,
    decimal DefaultRate,
    int? EntryId,
    decimal? QuantityLitres,
    decimal? Rate,
    decimal? Amount,
    string? Note);

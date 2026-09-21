namespace MilkHisab.Api.DTOs;

public record CustomerDto(
    int Id,
    string Name,
    string? Phone,
    string? Address,
    decimal DefaultRate,
    bool IsActive,
    DateTime CreatedAt);

public record CreateCustomerRequest(
    string Name,
    string? Phone,
    string? Address,
    decimal DefaultRate);

public record UpdateCustomerRequest(
    string Name,
    string? Phone,
    string? Address,
    decimal DefaultRate,
    bool IsActive);

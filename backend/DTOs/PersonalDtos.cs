namespace MilkHisab.Api.DTOs;

public record Credentials(string Name, string Password);
public record ProductRequest(string Name, string Unit, decimal Rate);
public record ExpenseRequest(DateOnly Date, string Title, string Category, string? Supplier,
    int? ProductId, decimal? Quantity, decimal? Rate, decimal Amount, decimal Paid, string Mode, string? Note);
public record ExpensePaymentRequest(DateOnly Date, decimal Amount, string Mode);

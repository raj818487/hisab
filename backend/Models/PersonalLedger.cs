namespace MilkHisab.Api.Models;

public class LedgerUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public class LedgerProduct
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "piece";
    public decimal Rate { get; set; }
}

public class Expense
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public DateOnly Date { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "Other";
    public string? Supplier { get; set; }
    public int? ProductId { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Rate { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class ExpensePayment
{
    public int Id { get; set; }
    public int ExpenseId { get; set; }
    public int OwnerId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public string Mode { get; set; } = "cash";
}

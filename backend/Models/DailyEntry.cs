namespace MilkHisab.Api.Models;

public class DailyEntry
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateOnly Date { get; set; }
    public decimal QuantityLitres { get; set; }
    public decimal Rate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }

    public decimal Amount => QuantityLitres * Rate;
}

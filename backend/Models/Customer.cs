namespace MilkHisab.Api.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public decimal DefaultRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DailyEntry> DailyEntries { get; set; } = new List<DailyEntry>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

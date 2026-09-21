namespace MilkHisab.Api.Models;

/// <summary>
/// Manual payment ledger entry only. Mode is a label (cash/upi/other), not a gateway.
/// No Razorpay/UPI SDK or online collection — record what was received offline.
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Label only: cash | upi | other — not a payment gateway.</summary>
    public string Mode { get; set; } = "cash";
    public DateOnly PaidOn { get; set; }
    public string? Note { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
}

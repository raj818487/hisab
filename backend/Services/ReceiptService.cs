using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MilkHisab.Api.Data;
using MilkHisab.Api.DTOs;

namespace MilkHisab.Api.Services;

/// <summary>
/// Receipt view + HTML download for manually recorded payments only.
/// No payment gateway. Month totals computed inline (no nested DbContext use).
/// </summary>
public class ReceiptService : IReceiptService
{
    private readonly AppDbContext _db;

    public ReceiptService(AppDbContext db) => _db = db;

    public async Task<string> NextReceiptNumberAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow;
        var prefix = $"MH-{today:yyyyMMdd}-";

        var last = await _db.Payments.AsNoTracking()
            .Where(p => p.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(p => p.ReceiptNumber)
            .Select(p => p.ReceiptNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (last is not null
            && last.Length > prefix.Length
            && int.TryParse(last.AsSpan(prefix.Length), out var n)
            && n >= 0)
        {
            seq = n + 1;
        }

        return $"{prefix}{seq:D4}";
    }

    public async Task<ReceiptDto?> GetReceiptAsync(int paymentId, CancellationToken ct = default)
    {
        var p = await _db.Payments.AsNoTracking()
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == paymentId, ct);
        if (p?.Customer is null) return null;

        var year = p.PaidOn.Year;
        var month = p.PaidOn.Month;
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);

        var delivered = await _db.DailyEntries.AsNoTracking()
            .Where(e => e.CustomerId == p.CustomerId && e.Date >= start && e.Date < end)
            .SumAsync(e => e.QuantityLitres * e.Rate, ct);

        var paid = await _db.Payments.AsNoTracking()
            .Where(x => x.CustomerId == p.CustomerId && x.PaidOn >= start && x.PaidOn < end)
            .SumAsync(x => x.Amount, ct);

        var due = delivered - paid;

        return new ReceiptDto(
            p.ReceiptNumber,
            p.Id,
            p.CustomerId,
            p.Customer.Name,
            p.Customer.Phone,
            p.Customer.Address,
            p.Amount,
            p.Mode,
            p.PaidOn,
            p.Note,
            DateTime.UtcNow,
            delivered,
            paid,
            due,
            year,
            month);
    }

    public async Task<string?> GetReceiptHtmlAsync(int paymentId, CancellationToken ct = default)
    {
        var r = await GetReceiptAsync(paymentId, ct);
        if (r is null) return null;
        return BuildHtml(r);
    }

    private static string BuildHtml(ReceiptDto r)
    {
        var culture = new CultureInfo("en-IN");
        string E(string? s) => WebUtility.HtmlEncode(s ?? "");
        var modeLabel = r.Mode switch
        {
            "upi" => "UPI (manual record)",
            "other" => "Other",
            _ => "Cash"
        };

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\"><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.AppendLine($"<title>Receipt {E(r.ReceiptNumber)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:system-ui,Segoe UI,sans-serif;max-width:640px;margin:24px auto;color:#0f172a;padding:0 16px;}");
        sb.AppendLine(".card{border:1px solid #99f6e4;border-radius:12px;padding:24px;background:#f0fdfa;}");
        sb.AppendLine("h1{color:#0f766e;margin:0 0 4px;font-size:1.5rem;}");
        sb.AppendLine(".muted{color:#64748b;font-size:.9rem;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;margin-top:16px;}");
        sb.AppendLine("th,td{text-align:left;padding:8px 4px;border-bottom:1px solid #ccfbf1;}");
        sb.AppendLine(".amount{font-size:1.75rem;font-weight:700;color:#0f766e;}");
        sb.AppendLine("@media print{body{margin:0;}.card{border:none;background:#fff;} .noprint{display:none;}}");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<div class=\"card\">");
        sb.AppendLine("<h1>Milk Hisab - Payment Receipt</h1>");
        sb.AppendLine("<p class=\"muted\">Manual payment record (no online gateway)</p>");
        sb.AppendLine($"<p><strong>Receipt #</strong> {E(r.ReceiptNumber)}</p>");
        sb.AppendLine($"<p class=\"amount\">Rs {r.Amount.ToString("N2", culture)}</p>");
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><th>Customer</th><td>{E(r.CustomerName)}</td></tr>");
        if (!string.IsNullOrWhiteSpace(r.CustomerPhone))
            sb.AppendLine($"<tr><th>Phone</th><td>{E(r.CustomerPhone)}</td></tr>");
        sb.AppendLine($"<tr><th>Paid on</th><td>{r.PaidOn:dd MMM yyyy}</td></tr>");
        sb.AppendLine($"<tr><th>Mode</th><td>{E(modeLabel)}</td></tr>");
        if (!string.IsNullOrWhiteSpace(r.Note))
            sb.AppendLine($"<tr><th>Note</th><td>{E(r.Note)}</td></tr>");
        sb.AppendLine($"<tr><th>Month ({r.Month:00}/{r.Year})</th><td>Delivered Rs {r.MonthDelivered.ToString("N2", culture)} | Paid Rs {r.MonthPaid.ToString("N2", culture)} | Due Rs {r.MonthDue.ToString("N2", culture)}</td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine($"<p class=\"muted\" style=\"margin-top:24px\">Generated {r.GeneratedAt:u} UTC</p>");
        sb.AppendLine("<p class=\"noprint\"><button onclick=\"window.print()\">Print / Save as PDF</button></p>");
        sb.AppendLine("</div></body></html>");
        return sb.ToString();
    }
}

using Microsoft.AspNetCore.Mvc;
using MilkHisab.Api.Contracts;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Services;

namespace MilkHisab.Api.Controllers;

/// <summary>
/// Receipt view + HTML download for manually recorded payments.
/// </summary>
[ApiController]
[Route("api/v1/receipts")]
public class ReceiptsController : ControllerBase
{
    private readonly IReceiptService _receipts;

    public ReceiptsController(IReceiptService receipts) => _receipts = receipts;

    [HttpGet("{paymentId:int}")]
    public async Task<ActionResult<ReceiptDto>> Get(int paymentId, CancellationToken ct)
    {
        var r = await _receipts.GetReceiptAsync(paymentId, ct);
        return r is null ? NotFound(new ApiMessage("Receipt not found.")) : Ok(r);
    }

    [HttpGet("{paymentId:int}/html")]
    public async Task<IActionResult> DownloadHtml(int paymentId, CancellationToken ct)
    {
        var html = await _receipts.GetReceiptHtmlAsync(paymentId, ct);
        if (html is null) return NotFound(new ApiMessage("Receipt not found."));

        var fileName = $"receipt-{paymentId}.html";
        return File(
            System.Text.Encoding.UTF8.GetBytes(html),
            "text/html; charset=utf-8",
            fileName);
    }
}

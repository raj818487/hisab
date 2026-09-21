using Microsoft.AspNetCore.Mvc;
using MilkHisab.Api.Contracts;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Services;

namespace MilkHisab.Api.Controllers;

/// <summary>
/// Manual payment ledger API. No Razorpay / UPI gateway — records only.
/// </summary>
[ApiController]
[Route("api/v1/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments) => _payments = payments;

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Record(
        [FromBody] RecordPaymentRequest request, CancellationToken ct)
    {
        if (request.CustomerId <= 0)
            return BadRequest(new ApiMessage("Select a customer."));
        if (request.Amount <= 0)
            return BadRequest(new ApiMessage("Amount must be greater than zero."));
        if (string.IsNullOrWhiteSpace(request.Mode))
            return BadRequest(new ApiMessage("Payment mode is required (cash / upi / other)."));

        try
        {
            var dto = await _payments.RecordAsync(request, ct);
            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiMessage(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiMessage(ex.Message));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentDto>> Get(int id, CancellationToken ct)
    {
        var p = await _payments.GetByIdAsync(id, ct);
        return p is null ? NotFound(new ApiMessage("Payment not found.")) : Ok(p);
    }

    [HttpGet("by-customer/{customerId:int}")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> ByCustomer(
        int customerId, [FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
        => Ok(await _payments.ListByCustomerAsync(customerId, year, month, ct));

    [HttpGet("by-month")]
    public async Task<ActionResult<IReadOnlyList<PaymentDto>>> ByMonth(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            return BadRequest(new ApiMessage("Month must be 1–12."));
        return Ok(await _payments.ListByMonthAsync(year, month, ct));
    }
}

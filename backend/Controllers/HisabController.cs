using Microsoft.AspNetCore.Mvc;
using MilkHisab.Api.Contracts;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Services;

namespace MilkHisab.Api.Controllers;

[ApiController]
[Route("api/v1/hisab")]
public class HisabController : ControllerBase
{
    private readonly IHisabService _hisab;

    public HisabController(IHisabService hisab) => _hisab = hisab;

    [HttpGet("month")]
    public async Task<ActionResult<HisabMonthSummaryDto>> Month(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (month is < 1 or > 12)
            return BadRequest(new ApiMessage("Month must be 1–12."));
        return Ok(await _hisab.GetMonthSummaryAsync(year, month, ct));
    }

    [HttpGet("customer/{customerId:int}")]
    public async Task<ActionResult<CustomerHisabDto>> CustomerMonth(
        int customerId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var row = await _hisab.GetCustomerMonthAsync(customerId, year, month, ct);
        return row is null
            ? NotFound(new ApiMessage("Customer hisab not found for that month."))
            : Ok(row);
    }
}

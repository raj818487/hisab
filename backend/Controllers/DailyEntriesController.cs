using Microsoft.AspNetCore.Mvc;
using MilkHisab.Api.Contracts;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Services;

namespace MilkHisab.Api.Controllers;

[ApiController]
[Route("api/v1/daily-entries")]
public class DailyEntriesController : ControllerBase
{
    private readonly IDailyEntryService _entries;

    public DailyEntriesController(IDailyEntryService entries) => _entries = entries;

    [HttpGet("day-board")]
    public async Task<ActionResult<IReadOnlyList<DayBoardItemDto>>> DayBoard(
        [FromQuery] DateOnly date, CancellationToken ct)
        => Ok(await _entries.GetDayBoardAsync(date, ct));

    [HttpGet("by-customer/{customerId:int}")]
    public async Task<ActionResult<IReadOnlyList<DailyEntryDto>>> ByCustomer(
        int customerId, [FromQuery] int? year, [FromQuery] int? month, CancellationToken ct)
        => Ok(await _entries.GetByCustomerAsync(customerId, year, month, ct));

    [HttpPost]
    public async Task<ActionResult<DailyEntryDto>> Upsert(
        [FromBody] UpsertDailyEntryRequest request, CancellationToken ct)
    {
        if (request.CustomerId <= 0)
            return BadRequest(new ApiMessage("Customer is required."));
        if (request.QuantityLitres < 0)
            return BadRequest(new ApiMessage("Quantity cannot be negative."));
        if (request.Rate < 0)
            return BadRequest(new ApiMessage("Rate cannot be negative."));

        try
        {
            var dto = await _entries.UpsertAsync(request, ct);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ApiMessage(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiMessage(ex.Message));
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => await _entries.DeleteAsync(id, ct)
            ? NoContent()
            : NotFound(new ApiMessage("Daily entry not found."));
}

using Microsoft.AspNetCore.Mvc;
using MilkHisab.Api.Contracts;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Services;

namespace MilkHisab.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers) => _customers = customers;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(
        [FromQuery] bool activeOnly = false, CancellationToken ct = default)
        => Ok(await _customers.GetAllAsync(activeOnly, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Get(int id, CancellationToken ct)
    {
        var c = await _customers.GetByIdAsync(id, ct);
        return c is null ? NotFound(new ApiMessage("Customer not found.")) : Ok(c);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ApiMessage("Name is required."));
        if (request.DefaultRate < 0)
            return BadRequest(new ApiMessage("Default rate cannot be negative."));

        var created = await _customers.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDto>> Update(
        int id, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new ApiMessage("Name is required."));
        if (request.DefaultRate < 0)
            return BadRequest(new ApiMessage("Default rate cannot be negative."));

        var updated = await _customers.UpdateAsync(id, request, ct);
        return updated is null ? NotFound(new ApiMessage("Customer not found.")) : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => await _customers.DeleteAsync(id, ct)
            ? NoContent()
            : NotFound(new ApiMessage("Customer not found."));
}

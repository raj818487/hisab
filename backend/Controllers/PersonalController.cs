using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Services;

namespace MilkHisab.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/personal")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class PersonalController(IPersonalService svc) : ControllerBase
{
    int OwnerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet] public async Task<IActionResult> Get() => Ok(await svc.GetAsync(OwnerId));

    [HttpPost("products")]
    public Task<IActionResult> AddProduct([FromBody] ProductRequest body) =>
        Run(async () => await svc.AddProductAsync(OwnerId, body));

    [HttpDelete("products/{id:int}")]
    public Task<IActionResult> DeleteProduct(int id) =>
        Run(async () => { await svc.DeleteProductAsync(OwnerId, id); return new { deleted = true }; });

    [HttpPost("expenses")]
    public Task<IActionResult> AddExpense([FromBody] ExpenseRequest body) =>
        Run(async () => await svc.AddExpenseAsync(OwnerId, body));

    [HttpPut("expenses/{id:int}")]
    public Task<IActionResult> UpdateExpense(int id, [FromBody] ExpenseRequest body) =>
        Run(async () => await svc.UpdateExpenseAsync(OwnerId, id, body));

    [HttpDelete("expenses/{id:int}")]
    public Task<IActionResult> DeleteExpense(int id) =>
        Run(async () => { await svc.DeleteExpenseAsync(OwnerId, id); return new { deleted = true }; });

    [HttpPost("expenses/{id:int}/payments")]
    public Task<IActionResult> Pay(int id, [FromBody] ExpensePaymentRequest body) =>
        Run(async () => await svc.PayAsync(OwnerId, id, body));

    async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

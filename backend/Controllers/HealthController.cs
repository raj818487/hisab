using Microsoft.AspNetCore.Mvc;

namespace MilkHisab.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        service = "MilkHisab.Api",
        payments = "manual-ledger-only",
        utc = DateTime.UtcNow
    });
}

/// <summary>Also expose /health for smoke scripts that omit the api/v1 prefix.</summary>
[ApiController]
[Route("health")]
public class RootHealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", service = "MilkHisab.Api" });
}

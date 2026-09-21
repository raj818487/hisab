using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Services;
namespace MilkHisab.Api.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/account")]
public class AccountController(IAccountService accounts) : ControllerBase
{
    [HttpGet] public IActionResult Session() => User.Identity?.IsAuthenticated == true ? Ok(new { name = User.Identity.Name }) : Unauthorized();
    [EnableRateLimiting("account")]
    [HttpPost("register")] public Task<IActionResult> Register(Credentials r) => Authenticate(r, true);
    [EnableRateLimiting("account")]
    [HttpPost("login")] public Task<IActionResult> Login(Credentials r) => Authenticate(r, false);
    [HttpPost("logout")] public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return NoContent();
    }
    private async Task<IActionResult> Authenticate(Credentials r, bool register)
    {
        try
        {
            var user = await accounts.AuthenticateAsync(r, register);
            if (user is null) return Unauthorized(new { message = "Username or password is incorrect." });
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Name) }, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Ok(new { name = user.Name });
        }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
    }
}

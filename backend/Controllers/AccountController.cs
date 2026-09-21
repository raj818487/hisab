using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MilkHisab.Api.DTOs;
using MilkHisab.Api.Services;

namespace MilkHisab.Api.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/account")]
public class AccountController(IAccountService accounts, IJwtTokenService jwt) : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult Session() =>
        User.Identity?.IsAuthenticated == true
            ? Ok(new { name = User.Identity.Name })
            : Unauthorized();

    [EnableRateLimiting("account")]
    [HttpPost("register")]
    public Task<IActionResult> Register(Credentials body) => IssueTokens(body, register: true);

    [EnableRateLimiting("account")]
    [HttpPost("login")]
    public Task<IActionResult> Login(Credentials body) => IssueTokens(body, register: false);

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.RefreshToken))
            return Unauthorized(new { message = "Refresh token is required." });

        var principal = jwt.ValidateRefreshToken(body.RefreshToken);
        if (principal is null)
            return Unauthorized(new { message = "Refresh token is invalid or expired." });

        var name = principal.Identity?.Name
            ?? principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst("unique_name")?.Value;
        if (string.IsNullOrWhiteSpace(name))
            return Unauthorized(new { message = "Refresh token is invalid." });

        var user = await accounts.FindByNameAsync(name);
        if (user is null)
            return Unauthorized(new { message = "Account no longer exists." });

        var (access, expires) = jwt.CreateAccessToken(user);
        var refresh = jwt.CreateRefreshToken(user);
        return Ok(new AuthTokenResponse(access, refresh, expires, user.Name));
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();

    private async Task<IActionResult> IssueTokens(Credentials body, bool register)
    {
        try
        {
            var user = await accounts.AuthenticateAsync(body, register);
            if (user is null)
                return Unauthorized(new { message = "Username or password is incorrect." });

            var (access, expires) = jwt.CreateAccessToken(user);
            var refresh = jwt.CreateRefreshToken(user);
            return Ok(new AuthTokenResponse(access, refresh, expires, user.Name));
        }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
    }
}
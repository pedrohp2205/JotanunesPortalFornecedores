using Jotanunes.API.Shared.Extensions;
using Jotanunes.Application.DTOs.Auth;
using Jotanunes.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.External.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenDto>> Login([FromBody] LoginDto loginDto)
    {
        var token = await _authService.Login(loginDto);
        return Ok(token);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenDto>> Refresh([FromBody] RefreshTokenDto refreshTokenDto)
    {
        var token = await _authService.Refresh(refreshTokenDto);
        return Ok(token);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        await _authService.Logout(User.GetUserId());
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthenticatedUserDto>> Me()
    {
        var user = await _authService.GetAuthenticatedUser(User.GetUserId());
        return Ok(user);
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        await _authService.ChangePassword(User.GetUserId(), changePasswordDto);
        return NoContent();
    }
}

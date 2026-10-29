using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MRRS.Web.Contracts;
using MRRS.Web.Models;

namespace MRRS.Web.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Enter a username and password." });
        }

        var user = await _users.FindByNameAsync(request.UserName.Trim());
        if (user is null && request.UserName.Contains('@', StringComparison.Ordinal))
        {
            user = await _users.FindByEmailAsync(request.UserName.Trim());
        }

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Those credentials were not recognized, or the account is disabled." });
        }

        var result = await _signIn.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Those credentials were not recognized, or the account is disabled." });
        }

        return Ok(await ToDtoAsync(user));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Sign in to continue." });
        }

        return Ok(await ToDtoAsync(user));
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return ApiMapper.ToDto(user, roles.OrderBy(role => role).ToArray());
    }
}

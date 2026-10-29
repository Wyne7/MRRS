using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MRRS.Web.Contracts;
using MRRS.Web.Models;

namespace MRRS.Web.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.Admin)]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private static readonly Regex UserNamePattern = new(@"^[a-zA-Z0-9._-]{3,40}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly UserManager<ApplicationUser> _users;

    public UsersController(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List()
    {
        var users = await _users.Users
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.UserName)
            .ToListAsync();

        var dtos = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            dtos.Add(await ToDtoAsync(user));
        }

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> Get(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new { message = "That person doesn't exist." });
        }

        return Ok(await ToDtoAsync(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserWriteRequest request)
    {
        var error = Validate(request, passwordRequired: true);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Department = Clean(request.Department),
            IsActive = request.IsActive,
            EmailConfirmed = true
        };

        var created = await _users.CreateAsync(user, request.Password!);
        if (!created.Succeeded)
        {
            return BadRequest(new { message = IdentityMessage(created) });
        }

        var roleResult = await _users.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            return BadRequest(new { message = IdentityMessage(roleResult) });
        }

        return Ok(await ToDtoAsync(user));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, UserWriteRequest request)
    {
        var error = Validate(request, passwordRequired: false);
        if (error is not null)
        {
            return BadRequest(new { message = error });
        }

        var user = await _users.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound(new { message = "That person doesn't exist." });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.Id == currentUserId && !request.IsActive)
        {
            return BadRequest(new { message = "You can't disable your own account." });
        }

        if (user.Id == currentUserId && request.Role != AppRoles.Admin)
        {
            return BadRequest(new { message = "You can't remove your own administrator role." });
        }

        var existingRoles = await _users.GetRolesAsync(user);
        var wasActiveAdmin = user.IsActive && existingRoles.Contains(AppRoles.Admin);
        var willBeActiveAdmin = request.IsActive && request.Role == AppRoles.Admin;
        if (wasActiveAdmin && !willBeActiveAdmin && await ActiveAdminCountAsync(excludeUserId: user.Id) < 1)
        {
            return BadRequest(new { message = "Leave at least one active administrator." });
        }

        user.Email = request.Email.Trim();
        user.DisplayName = request.DisplayName.Trim();
        user.Department = Clean(request.Department);
        user.IsActive = request.IsActive;
        var updated = await _users.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            return BadRequest(new { message = IdentityMessage(updated) });
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var reset = await _users.ResetPasswordAsync(user, token, request.Password);
            if (!reset.Succeeded)
            {
                return BadRequest(new { message = IdentityMessage(reset) });
            }
        }

        var currentRole = existingRoles.FirstOrDefault(AppRoles.IsKnown);
        if (currentRole != request.Role)
        {
            if (existingRoles.Count > 0)
            {
                var removed = await _users.RemoveFromRolesAsync(user, existingRoles);
                if (!removed.Succeeded)
                {
                    return BadRequest(new { message = IdentityMessage(removed) });
                }
            }

            var added = await _users.AddToRoleAsync(user, request.Role);
            if (!added.Succeeded)
            {
                return BadRequest(new { message = IdentityMessage(added) });
            }
        }

        if (!request.IsActive)
        {
            await _users.UpdateSecurityStampAsync(user);
        }

        return Ok(await ToDtoAsync(user));
    }

    private async Task<int> ActiveAdminCountAsync(string excludeUserId)
    {
        var admins = await _users.GetUsersInRoleAsync(AppRoles.Admin);
        return admins.Count(user => user.IsActive && user.Id != excludeUserId);
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await _users.GetRolesAsync(user);
        return ApiMapper.ToDto(user, roles.OrderBy(role => role).ToArray());
    }

    private static string? Validate(UserWriteRequest request, bool passwordRequired)
    {
        if (passwordRequired && !UserNamePattern.IsMatch(request.UserName.Trim()))
        {
            return "Usernames are 3–40 characters: letters, numbers, dots, underscores, or hyphens.";
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return "Name and email are required.";
        }

        if (!AppRoles.IsKnown(request.Role))
        {
            return "Role must be Admin or Employee.";
        }

        if (passwordRequired && string.IsNullOrWhiteSpace(request.Password))
        {
            return "Set a password for the new account.";
        }

        return null;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string IdentityMessage(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));
}

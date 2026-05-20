using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Application.DTOs;
using PMS.Domain.Entities;
using PMS.Domain.Enums;
using PMS.Infrastructure.Data;
using System.Security.Claims;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // ✅ GET all users
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role,
                u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> CreateUser(CreateUserRequest request)
{
    var userExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
    if (userExists)
        return BadRequest(new { message = "Cet email est déjà utilisé" });

    var user = new User
    {
        Id = Guid.NewGuid(),
        FirstName = request.FirstName ?? "",
        LastName = request.LastName ?? "",
        Email = request.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = request.Role,
        IsActive = true
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return Ok(new 
    { 
        id = user.Id,
        firstName = user.FirstName,
        lastName = user.LastName,
        email = user.Email,
        role = user.Role,
        isActive = user.IsActive
    });
}

[HttpPut("{id}/disable")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DisableUser(Guid id)
{
    var user = await _context.Users.FindAsync(id);
    if (user == null)
        return NotFound(new { message = "User not found" });

    user.IsActive = false;
    await _context.SaveChangesAsync();

    return NoContent();
}

[HttpPut("{id}/enable")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> EnableUser(Guid id)
{
    var user = await _context.Users.FindAsync(id);
    if (user == null)
        return NotFound(new { message = "User not found" });

    user.IsActive = true;
    await _context.SaveChangesAsync();

    return NoContent();
}

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;

        if (!string.IsNullOrEmpty(request.Email))
            user.Email = request.Email;

     
        user.Role = request.Role;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.IsActive
        });
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("developers")]
    public async Task<IActionResult> GetDevelopers()
    {
        var developers = await _context.Users
            .Where(u => u.Role == Role.Developer)
            .Select(u => new
            {
                u.Id,
                u.Email
            })
            .ToListAsync();

        return Ok(developers);
    }


    [Authorize(Roles = "Manager")]
    [HttpGet("developers/with-projects")]
    public async Task<IActionResult> GetDevelopersWithProjects()
    {
        var developers = await _context.Users
            .Where(u => u.Role == Role.Developer)
            .Select(u => new
            {
                u.Id,
                u.Email,
                Projects = u.Projects.Select(p => new
                {
                    p.Id,
                    p.Name
                })
            })
            .ToListAsync();

        return Ok(developers);
    }


    [Authorize(Roles = "Manager")]
    [HttpGet("developers/{id}")]
    public async Task<IActionResult> GetDeveloperById(Guid id)
    {
        var dev = await _context.Users
            .Where(u => u.Id == id && u.Role == Role.Developer)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.IsActive,
                Projects = u.Projects.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.StartDate,
                    p.EndDate,
                    p.Status,
                    TaskCount = p.Tasks.Count(t => t.AssigneeId == u.Id),
                    DoneCount = p.Tasks.Count(t => t.AssigneeId == u.Id && t.Status == TaskStatus.Done)
                })
            })
            .FirstOrDefaultAsync();

        if (dev == null) return NotFound();
        return Ok(dev);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);

        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Role,
                u.IsActive
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound();

        return Ok(user);
    }


    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateCurrentUser(UpdateUserRequest request)
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        // Mettre à jour les champs
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;

        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;

        if (!string.IsNullOrEmpty(request.Email))
            user.Email = request.Email;

        // Changer le mot de passe si fourni
        if (!string.IsNullOrEmpty(request.CurrentPassword) && !string.IsNullOrEmpty(request.NewPassword))
        {
            // Vérifier l'ancien mot de passe
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return BadRequest("Current password is incorrect");

            // Hasher le nouveau mot de passe
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Role,
            user.IsActive
        });
    }

    [HttpPut("me/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim.Value);
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound(new { message = "User not found" });

        // Vérifier l'ancien mot de passe
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect" });

        // Vérifier que le nouveau mot de passe n'est pas vide
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "New password cannot be empty" });

        // Vérifier la longueur minimale
        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters" });

        // Hasher le nouveau mot de passe
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully" });
    }

}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using BCrypt.Net;
using PMS.Domain.Enums;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

using PMS.Application.DTOs;

namespace PMS.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProjectsController(AppDbContext context)
    {
        _context = context;
    }




    [Authorize(Roles = "Manager")]
    [HttpPost("{projectId}/members")]
    public async Task<IActionResult> AddMember(
    Guid projectId,
    AddProjectMemberRequest request)
    {
        var managerId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p =>
                p.Id == projectId &&
                p.ManagerId == managerId);

        if (project == null)
            return NotFound("Project not found or access denied");

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null || user.Role != Role.Developer)
            return BadRequest("Invalid developer");

        if (project.Members.Any(m => m.Id == user.Id))
            return BadRequest("User already in project");

        project.Members.Add(user);
        await _context.SaveChangesAsync();
        return NoContent();


    }

    [Authorize(Roles = "Manager")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyProjects()
    {
        var managerId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var projects = await _context.Projects
            .Where(p => p.ManagerId == managerId)
            .Select(p => new ProjectResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status
})
            .ToListAsync();

        return Ok(projects);
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> CreateProject(CreateProjectRequest request)
    {
        var managerId = Guid.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
        );

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ManagerId = managerId
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            project.Id,
            project.Name
        });
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("{projectId}/members")]
    public async Task<IActionResult> GetProjectMembers(Guid projectId)
    {
        var managerId = Guid.Parse(
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value
        );

        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p =>
                p.Id == projectId &&
                p.ManagerId == managerId);

        if (project == null)
            return NotFound("Project not found or access denied");

        var members = project.Members.Select(m => new
        {
            m.Id,
            m.Email
        });

        return Ok(members);
    }


    [Authorize(Roles = "Manager")]
    [HttpDelete("{projectId}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid userId)
    {
        var managerId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p =>
            p.Id == projectId &&
            p.ManagerId == managerId);

        if (project == null) return NotFound("Project not foud or access denied");

        var member = project.Members.FirstOrDefault(m => m.Id == userId);
        if (member == null) return NotFound("Member not in project");

        project.Members.Remove(member);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{projectId}")]
    public async Task<IActionResult> UpdateProject(Guid projectId, UpdateProjectRequest request)
    {
        var managerId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var project = await _context.Projects
            .FirstOrDefaultAsync(p =>
                p.Id == projectId &&
                p.ManagerId == managerId);

        if (project == null)
            return NotFound("Project not found or access denied");

        // UPDATE fields safely
        project.Name = request.Name ?? project.Name;
        project.Description = request.Description ?? project.Description;
        project.StartDate = request.StartDate ?? project.StartDate;
        project.EndDate = request.EndDate ?? project.EndDate;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            project.Id,
            project.Name,
            project.Description,
            project.StartDate,
            project.EndDate
        });
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{projectId}")]
    public async Task<IActionResult> DeleteProject(Guid projectId)
    {
        var managerId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var project = await _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p =>
                p.Id == projectId &&
                p.ManagerId == managerId);

        if (project == null)
            return NotFound("Project not found or access denied");

        // optional cleanup (important if no cascade delete configured)
        _context.Tasks.RemoveRange(project.Tasks);
        project.Members.Clear();

        _context.Projects.Remove(project);

        await _context.SaveChangesAsync();

        return NoContent();
    }


    // Endpoint pour que le développeur voie ses projets (ceux où il est membre)
    [Authorize(Roles = "Developer")]
    [HttpGet("my-projects")]
    public async Task<IActionResult> GetMyProjectsAsDeveloper()
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var projects = await _context.Projects
            .Where(p => p.Members.Any(m => m.Id == userId))
            .Select(p => new ProjectResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = p.Status
            })
            .ToListAsync();

        return Ok(projects);
    }

    // Endpoint pour que le développeur voie les détails d'un projet spécifique
    [Authorize(Roles = "Developer")]
    [HttpGet("{projectId}/developer")]
    public async Task<IActionResult> GetProjectForDeveloper(Guid projectId)
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p =>
                p.Id == projectId &&
                p.Members.Any(m => m.Id == userId));

        if (project == null)
            return NotFound("Project not found or access denied");

        var response = new
        {
            project.Id,
            project.Name,
            project.Description,
            project.StartDate,
            project.EndDate,
            project.Status,
            Members = project.Members.Select(m => new
            {
                m.Id,
                m.Email,
                m.FirstName,
                m.LastName
            })
        };

        return Ok(response);
    }

    // Endpoint pour que le développeur voie les tâches d'un projet spécifique
    [Authorize(Roles = "Developer")]
    [HttpGet("{projectId}/tasks")]
    public async Task<IActionResult> GetProjectTasksForDeveloper(Guid projectId)
    {
        var userId = Guid.Parse(
            User.FindFirst(ClaimTypes.NameIdentifier)!.Value
        );

        // Vérifier que le développeur est membre du projet
        var isMember = await _context.Projects
            .AnyAsync(p => p.Id == projectId && p.Members.Any(m => m.Id == userId));

        if (!isMember)
            return Forbid("You are not a member of this project");

        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId && t.AssigneeId == userId)
            .Select(t => new TaskResponse
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                Deadline = t.Deadline
            })
            .ToListAsync();

        return Ok(tasks);
    }

    [Authorize(Roles = "Developer")]
    [HttpGet("{projectId}/details")]
    public async Task<IActionResult> GetProjectDetailsForDeveloper(Guid projectId)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var project = await _context.Projects
            .Include(p => p.Manager)
            .Include(p => p.Members)
            .Include(p => p.Tasks) 
            .FirstOrDefaultAsync(p =>
                p.Id == projectId &&
                p.Members.Any(m => m.Id == userId));

        if (project == null)
            return NotFound("Project not found or access denied");

        var response = new
        {
            project.Id,
            project.Name,
            project.Description,
            project.StartDate,
            project.EndDate,
            project.Status,
            Manager = project.Manager != null ? new
            {
                project.Manager.Id,
                project.Manager.Email,
                project.Manager.FirstName,
                project.Manager.LastName
            } : null,
            Members = project.Members.Select(m => new
            {
                m.Id,
                m.Email,
                m.FirstName,
                m.LastName
            }),
            Tasks = project.Tasks
                .Where(t => t.AssigneeId == userId)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status,
                    t.Priority,
                    t.Deadline
                })
        };

        return Ok(response);
    }


}
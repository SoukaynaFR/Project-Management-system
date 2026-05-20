
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
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _context;

    public TasksController(AppDbContext context)
    {
        _context = context;
    }



    [Authorize(Roles = "Manager")]
    [HttpGet("project/{projectId}")]
    public async Task<IActionResult> GetProjectTasks(Guid projectId)
    {
        var managerId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var project = await _context.Projects
            .FirstOrDefaultAsync(p =>
                p.Id == projectId &&
                p.ManagerId == managerId);

        if (project == null)
            return NotFound("Project not found or access denied");

        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .Select(t => new TaskResponse
            {
                Id = t.Id,
                Title = t.Title,
                Status = t.Status,
                Priority = t.Priority,
                Deadline = t.Deadline,
                AssigneeId = t.AssigneeId,
                ProjectId = t.ProjectId
            })
            .ToListAsync();

        return Ok(tasks);
    }

  

    // ✅ Manager crée une tâche
    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<IActionResult> CreateTask(CreateTaskRequest request)
    {
        var projectExists = await _context.Projects
            .AnyAsync(p => p.Id == request.ProjectId);

        if (!projectExists)
            return NotFound("Project not found");

        var isMember = await _context.Projects
            .Where(p => p.Id == request.ProjectId)
            .SelectMany(p => p.Members)
            .AnyAsync(m => m.Id == request.AssigneeId);

        if (!isMember)
            return BadRequest("Developer is not part of the project");

        // 3️⃣ Créer la tâche UNIQUEMENT si les règles passent
        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Deadline = request.Deadline,
            Status = TaskStatus.Todo,
            AssigneeId = request.AssigneeId,
            ProjectId = request.ProjectId
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();


        return NoContent(); // HTTP 204

    }

    [Authorize(Roles = "Developer")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyTasks()
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var tasks = await _context.Tasks
            .Where(t => t.AssigneeId == userId)
            .ToListAsync();

        return Ok(tasks);
    }

    // ✅ Developer met à jour le status
    [Authorize(Roles = "Developer")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateTaskStatusRequest request)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound("Task not found");

        // ✅ RÈGLE MÉTIER CRUCIALE
        if (task.AssigneeId != userId)
            return Forbid();

        task.Status = request.Status;
        await _context.SaveChangesAsync();

        //return Ok("Status updated");
        return NoContent(); // HTTP 204
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask(
    Guid id,
    UpdateTaskRequest request)
    {
        var managerId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!
        );

        var task = await _context.Tasks
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null)
            return NotFound("Task not found");

        // ✅ sécurité : seul le manager du projet peut modifier
        if (task.Project.ManagerId != managerId)
            return Forbid();

        // ✅ vérifier membre si assigné
        if (request.AssigneeId.HasValue)
        {
            var isMember = await _context.Projects
                .Where(p => p.Id == task.ProjectId)
                .SelectMany(p => p.Members)
                .AnyAsync(m => m.Id == request.AssigneeId.Value);

            if (!isMember)
                return BadRequest(
                    "Developer is not part of the project"
                );
        }

        // ✅ update
        task.Title = request.Title;
        task.Description = request.Description;
        task.Priority = request.Priority;
        task.Deadline = request.Deadline;
        task.AssigneeId = (Guid)request.AssigneeId;

        await _context.SaveChangesAsync();

        return NoContent();
    }

}
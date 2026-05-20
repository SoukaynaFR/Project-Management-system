using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;

namespace PMS.Application.DTOs;

public class UpdateTaskRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Priority Priority { get; set; }

    public DateTime Deadline { get; set; }

    public Guid? AssigneeId { get; set; }
}
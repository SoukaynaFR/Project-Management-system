
using TaskStatus = PMS.Domain.Enums.TaskStatus;
using PMS.Domain.Enums;

namespace PMS.Application.DTOs;


public class CreateTaskRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public Priority Priority { get; set; }
    public DateTime Deadline { get; set; }
    public Guid AssigneeId { get; set; }
    public Guid ProjectId { get; set; }
}
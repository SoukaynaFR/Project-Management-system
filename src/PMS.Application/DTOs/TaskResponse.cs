using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;


namespace PMS.Application.DTOs;

public class TaskResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public TaskStatus Status { get; set; }
    public string? Description { get; set; }

    public Priority Priority { get; set; }
    public DateTime Deadline { get; set; }

    public Guid AssigneeId { get; set; }
    public Guid ProjectId { get; set; }

}
using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;
namespace PMS.Domain.Entities;


public class TaskItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public Priority Priority { get; set; }
    public DateTime Deadline { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid AssigneeId { get; set; }
    public User Assignee { get; set; } = null!;
}

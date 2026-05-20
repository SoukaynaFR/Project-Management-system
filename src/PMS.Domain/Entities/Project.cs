using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;
namespace PMS.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }

    public Guid ManagerId { get; set; }
    public User Manager { get; set; } = null!;

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

    public ICollection<User> Members { get; set; } = new List<User>();

}

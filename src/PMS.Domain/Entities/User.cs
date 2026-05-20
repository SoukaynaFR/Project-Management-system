using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;
namespace PMS.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public Role Role { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();


    public ICollection<Project> Projects { get; set; } = new List<Project>();



}

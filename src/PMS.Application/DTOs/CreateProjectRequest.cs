using ProjectStatus = PMS.Domain.Enums.ProjectStatus;
namespace PMS.Application.DTOs;

    public class CreateProjectRequest
{
    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public string? Description { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }
}
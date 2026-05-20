namespace PMS.Application.DTOs;

public class UpdateProjectRequest
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
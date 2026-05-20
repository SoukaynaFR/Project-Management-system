using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;


namespace PMS.Application.DTOs;

public class AddProjectMemberRequest
{
    public Guid UserId { get; set; }
}

using PMS.Domain.Enums;
using TaskStatus = PMS.Domain.Enums.TaskStatus;


namespace PMS.Application.DTOs;

public class UpdateTaskStatusRequest
{
    public TaskStatus Status { get; set; }
}

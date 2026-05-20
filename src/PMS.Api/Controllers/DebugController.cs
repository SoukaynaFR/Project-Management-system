using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using BCrypt.Net;
using PMS.Domain.Enums;
using PMS.Application.DTOs;

namespace PMS.Api.Controllers;


[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    [Authorize]
    [HttpGet("headers")]
    public IActionResult Headers()
    {
        return Ok(Request.Headers);
    }
}
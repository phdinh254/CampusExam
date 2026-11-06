using Microsoft.AspNetCore.Authorization;
using CampusExam.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "CampusExam API đang hoạt động"
        });
    }

    [HttpGet("database")]
    public async Task<IActionResult> CheckDatabase(
        [FromServices] CampusExamDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connected = await dbContext.Database
            .CanConnectAsync(cancellationToken);

        if (!connected)
        {
            return StatusCode(503, new
            {
                message = "Chưa kết nối được database"
            });
        }

        return Ok(new
        {
            message = "Kết nối database thành công"
        });
    }
}
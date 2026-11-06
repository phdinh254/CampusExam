using CampusExam.Application.MucDo;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Controllers;

[ApiController]
[Route("api/muc-do")]
public class MucDoController(
    IMucDoQuery mucDoQuery) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MucDoDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var danhSach = await mucDoQuery.GetAllAsync(cancellationToken);

        return Ok(danhSach);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/tests")]
[Authorize]
public class TestController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok("Api is working perfectly");
    }
}
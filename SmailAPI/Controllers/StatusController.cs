using Microsoft.AspNetCore.Mvc;

namespace SmailAPI.Controllers;

[ApiController]
[Route("api/")]
public class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return Ok("Webhook API is running!");
    }
}

using Microsoft.AspNetCore.Mvc;

namespace DocxToPdf.Controllers;

[ApiController]
[Route("/")]
public class HealthCheckController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "Healthy" });
    }
}
using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.Internal.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        return Ok(new
        {
            Message = "Bem vindo a API interna da Jotanunes",
            AccessDate = DateTime.Now.ToLongDateString(),
        });
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Jotanunes.API.External.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult Get()
    {
        return Ok(new
        {
            Message = "Bem vindo a API do Portal do Fornecedor",
            AccessDate = DateTime.Now.ToLongDateString(),
        });
    }
}

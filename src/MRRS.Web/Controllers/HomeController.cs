using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MRRS.Web.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    [HttpGet("/host")]
    public IActionResult Index() => View();

    [AllowAnonymous]
    [HttpGet("/api/health")]
    public IActionResult Health() => Ok(new { status = "ok", service = "MRRS" });
}

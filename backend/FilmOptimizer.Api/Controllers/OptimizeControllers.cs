using FilmOptimizer.Api.Services;
using FilmOptimizer.Shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace FilmOptimizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OptimizeController : ControllerBase
{
    private readonly IPackingService _service;

    public OptimizeController(IPackingService service)
    {
        _service = service;
    }

    [HttpPost]
    public IActionResult Optimize(OptimizeRequest request)
    {
        return Ok(_service.Optimize(request));
    }
}
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
    public IActionResult Optimize([FromBody] OptimizeRequest request)
    {
        try
        {
            return Ok(_service.Optimize(request));
        }
        catch (ArgumentException ex)
        {
            // 잘못된 입력 (폭 0, 조각이 폭에 안 들어감 등)
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // 입력은 유효하지만 최대 길이 안에 담을 수 없음
            return UnprocessableEntity(new { message = ex.Message });
        }
    }
}

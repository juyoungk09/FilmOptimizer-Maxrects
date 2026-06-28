using FilmOptimizer.Shared.Requests;
using FilmOptimizer.Shared.Responses;

namespace FilmOptimizer.Api.Services;

public interface IPackingService
{
    OptimizeResponse Optimize(OptimizeRequest request);
}
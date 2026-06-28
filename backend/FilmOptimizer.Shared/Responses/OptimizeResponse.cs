namespace FilmOptimizer.Shared.Responses;

public class OptimizeResponse
{
    public int UsedLength { get; set; }

    public double WasteRate { get; set; }

    public List<PlacementDto> Placements { get; set; } = [];
}
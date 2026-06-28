namespace FilmOptimizer.Core.Models;

public class PackingResult
{
    public int UsedLength { get; init; }

    public double WasteRate { get; init; }

    public List<Placement> Placements { get; init; } = [];
}
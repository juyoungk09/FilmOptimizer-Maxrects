namespace FilmOptimizer.Core.Models;

using FilmOptimizer.Core.Algorithms.MaxRects;

public class PackingOptions
{
    public int FilmWidth { get; init; }

    public int InitialHeight { get; init; } = 500;

    public int Gap { get; init; } = 0;

    public bool AllowRotate { get; init; } = true;

    public Heuristic Heuristic { get; init; } = Heuristic.BestAreaFit;
}
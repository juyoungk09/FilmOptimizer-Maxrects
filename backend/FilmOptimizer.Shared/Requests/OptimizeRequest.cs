namespace FilmOptimizer.Shared.Requests;

public class OptimizeRequest
{
    public int FilmWidth { get; set; }

    public int Gap { get; set; }

    public bool AllowRotate { get; set; }

    public List<PieceDto> Pieces { get; set; } = [];
}
namespace FilmOptimizer.Core.Models;

public class Placement
{
    public int PieceId { get; }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public bool Rotated { get; }

    public Placement(
        int pieceId,
        int x,
        int y,
        int width,
        int height,
        bool rotated)
    {
        PieceId = pieceId;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Rotated = rotated;
    }
}
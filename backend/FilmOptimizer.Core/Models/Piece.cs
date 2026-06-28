namespace FilmOptimizer.Core.Models;

public class Piece
{
    public int Id { get; }

    public int Width { get; }

    public int Height { get; }

    public int Area => Width * Height;

    public Piece(int id, int width, int height)
    {
        Id = id;
        Width = width;
        Height = height;
    }
}
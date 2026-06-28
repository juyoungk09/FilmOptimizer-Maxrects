namespace FilmOptimizer.Core.Models;

public class FreeRectangle
{
    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    public FreeRectangle(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Fits(int width, int height)
    {
        return Width >= width && Height >= height;
    }
}
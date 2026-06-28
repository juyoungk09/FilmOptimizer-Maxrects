namespace FilmOptimizer.Core.Geometry;

public class Rect
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public Rect() {

    }

    public Rect(int x, int y, int width, int height) {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Fits(int w, int h) {
        return Width >= w && Height >= h;
    }
}

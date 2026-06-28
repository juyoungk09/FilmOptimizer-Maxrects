namespace FilmOptimizer.Tests;

using FilmOptimizer.Core.Algorithms.MaxRects;
using FilmOptimizer.Core.Models;
using Xunit;

public class UnitTest1
{
    [Fact]
    public void SinglePiece_ShouldBePlaced()
    {
    var options = new PackingOptions
    {
        FilmWidth = 1200,
        InitialHeight = 500,
        AllowRotate = true,
        Gap = 2
    };

    var packer = new MaxRectsPacker(options);

    var result = packer.Pack(new[]
    {
        new Piece(1, 100, 100)
    });

    Assert.Single(result.Placements);
    }
}
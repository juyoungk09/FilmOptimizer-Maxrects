namespace FilmOptimizer.Tests;

using FilmOptimizer.Core.Algorithms.MaxRects;
using FilmOptimizer.Core.Models;
using Xunit;

/// <summary>
/// 목표: 폭(FilmWidth)은 고정이고, 사용 길이(UsedLength)를 최소화한다.
/// </summary>
public class PackingTests
{
    private static PackingOptions Options(
        int filmWidth,
        int gap = 0,
        bool allowRotate = true,
        int? maxLength = null) => new()
        {
            FilmWidth = filmWidth,
            InitialHeight = 100,
            Gap = gap,
            AllowRotate = allowRotate,
            MaxLength = maxLength
        };

    /// <summary>폭이 고정되어 있으므로 길이는 넓이의 하한보다 짧을 수 없다.</summary>
    private static int TheoreticalMinLength(int filmWidth, IEnumerable<Piece> pieces)
    {
        long area = pieces.Sum(p => p.Area);

        return (int)Math.Ceiling((double)area / filmWidth);
    }

    [Fact]
    public void SinglePiece_ShouldBePlaced()
    {
        var packer = new MaxRectsPacker(Options(filmWidth: 1200));

        var result = packer.Pack(new[] { new Piece(1, 100, 100) });

        Assert.Single(result.Placements);
    }

    [Fact]
    public void UsedLength_IsNeverShorterThanTheAreaLowerBound()
    {
        var pieces = new[]
        {
            new Piece(1, 200, 150),
            new Piece(2, 180, 120),
            new Piece(3, 90, 90),
            new Piece(4, 60, 40),
        };

        var packer = new MaxRectsPacker(Options(filmWidth: 300));

        var result = packer.Pack(pieces);

        Assert.True(
            result.UsedLength >= TheoreticalMinLength(300, pieces),
            $"사용 길이 {result.UsedLength}가 넓이 하한 " +
            $"{TheoreticalMinLength(300, pieces)}보다 작음");
    }

    [Fact]
    public void Pieces_AreLaidOutSideBySideAcrossTheFixedWidth()
    {
        // 폭 1000에 500x500 조각 2개는 나란히 들어간다.
        // 세로로 쌓으면 길이 1000이 필요하지만 나란히면 500이면 충분하다.
        var pieces = new[]
        {
            new Piece(1, 500, 500),
            new Piece(2, 500, 500),
        };

        var packer = new MaxRectsPacker(Options(filmWidth: 1000));

        var result = packer.Pack(pieces);

        Assert.Equal(500, result.UsedLength);
    }

    [Fact]
    public void AllPlacements_StayInsideTheFilmWidth()
    {
        var packer = new MaxRectsPacker(Options(filmWidth: 400));

        var result = packer.Pack(new[]
        {
            new Piece(1, 130, 70),
            new Piece(2, 90, 45),
            new Piece(3, 55, 200),
            new Piece(4, 300, 60),
        });

        Assert.NotEmpty(result.Placements);

        foreach (var placement in result.Placements)
        {
            Assert.True(
                placement.X >= 0,
                $"조각 {placement.PieceId}의 X가 음수: {placement.X}");

            Assert.True(
                placement.X + placement.Width <= 400,
                $"조각 {placement.PieceId}가 필름 폭을 벗어남: " +
                $"{placement.X}+{placement.Width} > 400");
        }
    }

    [Fact]
    public void AllPlacements_AreInsideTheReportedUsedLength()
    {
        var packer = new MaxRectsPacker(Options(filmWidth: 300));

        var result = packer.Pack(new[]
        {
            new Piece(1, 200, 150),
            new Piece(2, 180, 120),
            new Piece(3, 90, 90),
        });

        foreach (var placement in result.Placements)
        {
            Assert.True(
                placement.Y + placement.Height <= result.UsedLength,
                $"조각 {placement.PieceId}가 사용 길이 밖: " +
                $"{placement.Y}+{placement.Height} > {result.UsedLength}");
        }
    }

    [Fact]
    public void Placements_DoNotOverlap()
    {
        int gap = 3;

        var packer = new MaxRectsPacker(Options(filmWidth: 350, gap: gap));

        var result = packer.Pack(new[]
        {
            new Piece(1, 120, 80),
            new Piece(2, 90, 90),
            new Piece(3, 70, 140),
            new Piece(4, 200, 50),
            new Piece(5, 60, 60),
        });

        var placements = result.Placements;

        for (int i = 0; i < placements.Count; i++)
        {
            for (int j = i + 1; j < placements.Count; j++)
            {
                var a = placements[i];
                var b = placements[j];

                bool separated =
                    a.X + a.Width + gap <= b.X ||
                    b.X + b.Width + gap <= a.X ||
                    a.Y + a.Height + gap <= b.Y ||
                    b.Y + b.Height + gap <= a.Y;

                Assert.True(
                    separated,
                    $"조각 {a.PieceId}와 {b.PieceId}가 간격 {gap}를 두고 겹치지 않음");
            }
        }
    }

    [Fact]
    public void Rotation_IsUsedToFitAcrossTheWidth()
    {
        // 300 폭에 500x200 조각은 회전해야만 들어간다.
        var packer = new MaxRectsPacker(Options(filmWidth: 300, allowRotate: true));

        var result = packer.Pack(new[] { new Piece(1, 500, 200) });

        var placement = Assert.Single(result.Placements);

        Assert.True(placement.Rotated);
        Assert.Equal(200, placement.Width);
        Assert.Equal(500, placement.Height);
    }

    [Fact]
    public void WithoutRotation_PieceWiderThanFilmIsRejected()
    {
        var packer = new MaxRectsPacker(Options(filmWidth: 300, allowRotate: false));

        Assert.Throws<ArgumentException>(() =>
            packer.Pack(new[] { new Piece(1, 500, 200) }));
    }

    [Fact]
    public void PieceThatCannotFitEvenRotated_IsRejected()
    {
        // 짧은 변이 300보다 크면 회전해도 폭에 들어가지 않는다.
        var packer = new MaxRectsPacker(Options(filmWidth: 300, allowRotate: true));

        Assert.Throws<ArgumentException>(() =>
            packer.Pack(new[] { new Piece(1, 400, 400) }));
    }

    [Fact]
    public void ExceedingMaxLength_ThrowsInsteadOfLoopingForever()
    {
        var packer = new MaxRectsPacker(
            Options(filmWidth: 200, maxLength: 250));

        Assert.Throws<InvalidOperationException>(() =>
            packer.Pack(new[]
            {
                new Piece(1, 150, 150),
                new Piece(2, 150, 150),
                new Piece(3, 150, 150),
            }));
    }

    [Fact]
    public void EveryHeuristic_ProducesAValidLayout()
    {
        var pieces = new[]
        {
            new Piece(1, 200, 150),
            new Piece(2, 180, 120),
            new Piece(3, 90, 90),
            new Piece(4, 60, 40),
        };

        foreach (var heuristic in Enum.GetValues<Heuristic>())
        {
            var packer = new MaxRectsPacker(new PackingOptions
            {
                FilmWidth = 300,
                InitialHeight = 100,
                Heuristic = heuristic
            });

            var result = packer.Pack(pieces);

            Assert.Equal(4, result.Placements.Count);

            foreach (var placement in result.Placements)
            {
                Assert.True(placement.X >= 0);
                Assert.True(placement.X + placement.Width <= 300);
                Assert.True(placement.Y >= 0);
            }
        }
    }

    [Fact]
    public void EmptyInput_ProducesZeroLength()
    {
        var packer = new MaxRectsPacker(Options(filmWidth: 300));

        var result = packer.Pack(Array.Empty<Piece>());

        Assert.Equal(0, result.UsedLength);
        Assert.Equal(0, result.WasteRate);
        Assert.Empty(result.Placements);
    }

    [Fact]
    public void WasteRate_IsZeroWhenPiecesFillTheFilmExactly()
    {
        // 200 폭에 200x150 조각 하나 = 30000 / (200*150) = 1.0
        var packer = new MaxRectsPacker(Options(filmWidth: 200));

        var result = packer.Pack(new[] { new Piece(1, 200, 150) });

        Assert.Equal(150, result.UsedLength);
        Assert.Equal(0, result.WasteRate, 6);
    }

    [Fact]
    public void DefaultHeuristic_MatchesTheMeasuredBestTotal()
    {
        // 정렬 키를 실측해서 고른 결과(5개 인스턴스 사용 길이 합):
        //   아래쪽 Y 1순위 (BottomLeft, 현재 기본값) → 2752
        //   위쪽 Y 1순위                            → 3025
        //   빈틈(넓이) 1순위                        → 3199
        //   짧은 변 1순위                            → 3404
        // 즉 폭 고정·길이 최소화 목표에서는 "조각이 닿는 가장 아래쪽 Y 최소화"가
        // 1순위여야 필름이 실제로 짧아진다. 빈틈 최소화를 1순위로 두면 위쪽
        // 빈틈부터 메우다 중간에 못 쓰는 구멍을 남겨 결과적으로 더 길어진다.
        // 이 값은 회귀 감시용이다. 정렬 키를 바꾸면 반드시 다시 측정할 것.
        int[] totals =
        [
            655,  // rand40-w400
            377,  // small120-w500
            645,  // mixed-w600
            608,  // long25-w350
            467,  // square60-w450
        ];

        var actuals = new int[totals.Length];

        for (int i = 0; i < totals.Length; i++)
        {
            var (pieces, width) = BenchmarkInstance(i);

            var packer = new MaxRectsPacker(new PackingOptions
            {
                FilmWidth = width,
                InitialHeight = 200
            });

            actuals[i] = packer.Pack(pieces).UsedLength;
        }

        Assert.Equal(totals, actuals);
    }

    [Fact]
    public void Pieces_DoNotOverlap_WhenFreeSpaceRunsOut()
    {
        // 폭을 꽉 채우는 조각을 놓으면 자유 사각형이 하나도 안 남는다.
        // 이때 밴드를 필름 끝이 아니라 y=0 에 붙이면 이미 놓은 조각 위로
        // 겹쳐 쌓인다(실제로 폐기율이 음수가 되던 버그).
        var packer = new MaxRectsPacker(Options(filmWidth: 200));

        var result = packer.Pack(new[]
        {
            new Piece(1, 200, 150),
            new Piece(2, 200, 150),
            new Piece(3, 200, 90)
        });

        AssertNoOverlap(result.Placements);

        // 겹쳤으면 사용 길이 x 폭이 조각 넓이 합보다 커져 폐기율이 음수가 된다.
        // (버그가 있었을 때는 -160% 가 나왔다)
        Assert.True(result.WasteRate >= 0, $"폐기율이 {result.WasteRate} 로 음수다.");

        // 390 은 넓이 하한(200*390). 밴드 높이 때문에 실제로는 490 이 된다.
        Assert.True(result.UsedLength >= 390, $"사용 길이 {result.UsedLength} 가 넓이 하한보다 짧다.");
    }

    [Fact]
    public void SpaceBelowAPiece_IsReused_InsteadOfAbandoned()
    {
        // 좁은 필름에 큰 조각을 먼저 놓으면 조각 "옆"의 빈 공간이 남는다.
        // 그 빈 공간을 버리고 매번 새 밴드를 붙이면 길이가 몇 배로 늘어난다.
        // InitialHeight 은 PackingService 기본값(500)과 맞춘다.
        var packer = new MaxRectsPacker(new PackingOptions
        {
            FilmWidth = 400,
            InitialHeight = 500
        });

        var pieces = new List<Piece>();

        for (int i = 0; i < 14; i++)
            pieces.Add(new Piece(i + 1, 90 + i * 37 % 70, 60 + i * 53 % 50));

        var result = packer.Pack(pieces);

        // 넓이 하한은 340 이다. 버그가 있었을 때는 2060 까지 늘었다.
        Assert.True(
            result.UsedLength <= 400,
            $"사용 길이 {result.UsedLength} 가 400 을 넘었다.");

        AssertNoOverlap(result.Placements);
    }

    [Fact]
    public void RotatingIsNotBiasedByTheLeftoverMetric()
    {
        // Leftover 를 "바운딩 박스 남은 넓이"로 계산하면 조각 넓이가 같은 두
        // 방향의 값이 달라져, 회전하지 않는 방향을 잘못 고른다.
        // 넓이(바운딩 박스 아님)로 계산하면 두 방향 값이 같아야 한다.
        var packer = new MaxRectsPacker(Options(filmWidth: 1200, gap: 2));

        var result = packer.Pack(new[]
        {
            new Piece(1, 81, 152),
            new Piece(2, 81, 152),
            new Piece(3, 31, 40),
            new Piece(4, 31, 40)
        });

        // 81x152 조각을 90 도 돌려 152x81 로 옆으로 나란히 놓을 수 있다.
        // 두 조각을 세로로 쌓으면 81+2+81 = 164 가 된다.
        Assert.Equal(81, result.UsedLength);
    }

    private static void AssertNoOverlap(IReadOnlyList<Placement> placements)
    {
        for (int i = 0; i < placements.Count; i++)
        {
            for (int j = i + 1; j < placements.Count; j++)
            {
                var a = placements[i];
                var b = placements[j];

                bool separated =
                    a.X + a.Width <= b.X ||
                    b.X + b.Width <= a.X ||
                    a.Y + a.Height <= b.Y ||
                    b.Y + b.Height <= a.Y;

                Assert.True(
                    separated,
                    $"{a.PieceId} 와 {b.PieceId} 가 겹친다: " +
                    $"({a.X},{a.Y},{a.Width}x{a.Height}) / " +
                    $"({b.X},{b.Y},{b.Width}x{b.Height})");
            }
        }
    }

    /// <summary>정렬 키 측정 때 사용한 인스턴스. 순서를 바꾸면 안 된다.</summary>
    private static (List<Piece> Pieces, int Width) BenchmarkInstance(int index)
    {
        switch (index)
        {
            case 0:
            {
                var pieces = new List<Piece>();
                for (int i = 0; i < 40; i++)
                    pieces.Add(new Piece(i + 1, 40 + (i * 37) % 90, 30 + (i * 53) % 70));
                return (pieces, 400);
            }

            case 1:
            {
                var pieces = new List<Piece>();
                for (int i = 0; i < 120; i++)
                    pieces.Add(new Piece(i + 1, 20 + (i * 17) % 45, 15 + (i * 29) % 40));
                return (pieces, 500);
            }

            case 2:
            {
                var pieces = new List<Piece>
                {
                    new(1, 300, 250),
                    new(2, 280, 200),
                    new(3, 260, 180),
                };
                for (int i = 0; i < 30; i++)
                    pieces.Add(new Piece(i + 4, 50 + (i * 13) % 60, 40 + (i * 11) % 50));
                return (pieces, 600);
            }

            case 3:
            {
                var pieces = new List<Piece>();
                for (int i = 0; i < 25; i++)
                    pieces.Add(new Piece(i + 1, 180 + (i * 7) % 60, 25 + (i * 5) % 20));
                return (pieces, 350);
            }

            default:
            {
                var pieces = new List<Piece>();
                for (int i = 0; i < 60; i++)
                {
                    int s = 30 + (i * 19) % 50;
                    pieces.Add(new Piece(i + 1, s, s));
                }
                return (pieces, 450);
            }
        }
    }

    [Fact]
    public void EachPieceIsPlacedExactlyOnce()
    {
        var packer = new MaxRectsPacker(Options(filmWidth: 320));

        var result = packer.Pack(new[]
        {
            new Piece(7, 100, 100),
            new Piece(8, 60, 60),
            new Piece(9, 140, 40),
        });

        var ids = result.Placements.Select(p => p.PieceId).OrderBy(id => id).ToList();

        Assert.Equal(new[] { 7, 8, 9 }, ids);
    }
}

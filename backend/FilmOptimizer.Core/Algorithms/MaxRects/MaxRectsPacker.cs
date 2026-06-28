using FilmOptimizer.Core.Models;

namespace FilmOptimizer.Core.Algorithms.MaxRects;

public class MaxRectsPacker
{
    private readonly PackingOptions _options;

    private readonly List<FreeRectangle> _freeRectangles = new();
    private readonly List<Placement> _placements = new();

    public IReadOnlyList<Placement> Placements => _placements;

    public MaxRectsPacker(PackingOptions options)
    {
        _options = options;

        _freeRectangles.Add(
            new FreeRectangle(
                0,
                0,
                options.FilmWidth,
                options.InitialHeight
            )
        );
    }
    public PackingResult Pack(IEnumerable<Piece> pieces)
    {
        // 큰 것부터
        var ordered = pieces
            .OrderByDescending(p => p.Area)
            .ThenByDescending(p => Math.Max(p.Width, p.Height))
            .ThenByDescending(p => Math.Min(p.Width, p.Height))
            .ToList();

        foreach (var piece in ordered)
        {
            while (!Insert(piece))
            {
                ExpandHeight();
            }
        }

        int usedLength = CalculateUsedLength();

        return new PackingResult
        {
            UsedLength = usedLength,
            WasteRate = CalculateWasteRate(usedLength),
            Placements = _placements.ToList()
        };
    }
    public bool Insert(Piece piece)
    {
        var result = FindBestPosition(piece);

        if (result.Rect == null)
            return false;

        int width = result.Rotated ? piece.Height : piece.Width;
        int height = result.Rotated ? piece.Width : piece.Height;

        var placement = new Placement(
            piece.Id,
            result.Rect.X,
            result.Rect.Y,
            width,
            height,
            result.Rotated
        );

        SplitFreeRectangles(placement);

        PruneFreeRectangles();

        _placements.Add(placement);

        return true;
    }
    private (FreeRectangle? Rect, bool Rotated) FindBestPosition(Piece piece)
    {
        FreeRectangle? best = null;
        bool rotated = false;

        int bestScore = int.MaxValue;

        foreach (var rect in _freeRectangles)
        {
            Evaluate(rect, piece.Width, piece.Height, false);

            if (_options.AllowRotate)
                Evaluate(rect, piece.Height, piece.Width, true);
        }

        return (best, rotated);

        void Evaluate(FreeRectangle rect, int width, int height, bool rotate)
        {
            width += _options.Gap;
            height += _options.Gap;

            if (!rect.Fits(width, height))
                return;

            int score = CalculateScore(rect, width, height);

            if (score < bestScore)
            {
                bestScore = score;
                best = rect;
                rotated = rotate;
            }
        }
    }
    private int CalculateScore(
        FreeRectangle rect,
        int width,
        int height)
    {
        return _options.Heuristic switch
        {
            Heuristic.BestAreaFit =>
                rect.Width * rect.Height - width * height,

            Heuristic.BestShortSideFit =>
                Math.Min(rect.Width - width, rect.Height - height),

            Heuristic.BestLongSideFit =>
                Math.Max(rect.Width - width, rect.Height - height),

            Heuristic.BottomLeft =>
                rect.Y * 100000 + rect.X,

            _ => int.MaxValue
        };
    }
    private void SplitFreeRectangles(Placement placed)
    {
        for (int i = 0; i < _freeRectangles.Count;)
        {
            if (SplitFreeNode(_freeRectangles[i], placed))
            {
                _freeRectangles.RemoveAt(i);
            }
            else
            {
                i++;
            }
        }
    }
    private bool SplitFreeNode(FreeRectangle free, Placement used)
    {
        int usedRight = used.X + used.Width + _options.Gap;
        int usedBottom = used.Y + used.Height + _options.Gap;
        // 겹치지 않으면 Split 안 함
        if (
            used.X >= free.X + free.Width ||
            usedRight <= free.X ||
            used.Y >= free.Y + free.Height ||
            usedBottom <= free.Y)
        {
            return false;
        }

        // 위쪽
        if (used.Y > free.Y)
        {
            _freeRectangles.Add(
                new FreeRectangle(
                    free.X,
                    free.Y,
                    free.Width,
                    used.Y - free.Y
                ));
        }

        // 아래쪽
        if (usedBottom < free.Y + free.Height)
{
            _freeRectangles.Add(
                new FreeRectangle(
                    free.X,
                    usedBottom,
                    free.Width,
                    free.Y + free.Height - usedBottom
                ));
        }

        // 왼쪽
        if (used.X > free.X)
        {
            _freeRectangles.Add(
                new FreeRectangle(
                    free.X,
                    free.Y,
                    used.X - free.X,
                    free.Height
                ));
        }

        // 오른쪽
        if (usedRight < free.X + free.Width)
        {
            _freeRectangles.Add(
                new FreeRectangle(
                    usedRight,
                    free.Y,
                    free.X + free.Width - usedRight,
                    free.Height
                ));
        }

        return true;
    }
    private void PruneFreeRectangles()
    {
        for (int i = 0; i < _freeRectangles.Count; i++)
        {
            for (int j = i + 1; j < _freeRectangles.Count; j++)
            {
                if (IsContainedIn(_freeRectangles[i], _freeRectangles[j]))
                {
                    _freeRectangles.RemoveAt(i);
                    i--;
                    break;
                }

                if (IsContainedIn(_freeRectangles[j], _freeRectangles[i]))
                {
                    _freeRectangles.RemoveAt(j);
                    j--;
                }
            }
        }
    }
    private static bool IsContainedIn(
        FreeRectangle a,
        FreeRectangle b)
    {
        return
            a.X >= b.X &&
            a.Y >= b.Y &&
            a.X + a.Width <= b.X + b.Width &&
            a.Y + a.Height <= b.Y + b.Height;
    }
    private void ExpandHeight()
    {
        var maxY = _freeRectangles.Max(r => r.Y + r.Height);

        _freeRectangles.Add(
            new FreeRectangle(
                0,
                maxY,
                _options.FilmWidth,
                _options.InitialHeight
            )
        );
    }
    private int CalculateUsedLength()
    {
        if (_placements.Count == 0)
            return 0;

        return _placements.Max(p => p.Y + p.Height);
    }
    private double CalculateWasteRate(int usedLength)
    {
        if (usedLength == 0)
            return 0;

        int pieceArea = _placements.Sum(p => p.Width * p.Height);

        int totalArea = _options.FilmWidth * usedLength;

        return 1.0 - (double)pieceArea / totalArea;
    }
}
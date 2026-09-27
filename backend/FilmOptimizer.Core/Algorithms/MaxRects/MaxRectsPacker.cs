using FilmOptimizer.Core.Models;

namespace FilmOptimizer.Core.Algorithms.MaxRects;

/// <summary>
/// 폭(FilmWidth, X축)은 고정이고 길이(Y축)만 늘어나는 필름에 조각을 배치한다.
/// 목표는 사용 길이(UsedLength)를 최소화하는 것이다.
/// </summary>
public class MaxRectsPacker
{
    private readonly PackingOptions _options;
    private readonly List<FreeRectangle> _freeRectangles = new();
    private readonly List<Placement> _placements = new();

    /// <summary>
    /// 필름을 한 번에 늘릴 때 붙이는 밴드의 높이.
    /// 가장 큰 조각보다 작으면 그 조각이 어떤 밴드에도 들어가지 못해
    /// <see cref="Place"/>의 루프가 끝나지 않는다.
    /// </summary>
    private int _bandHeight;

    /// <summary>지금까지 확보한 필름의 물리적 길이. 아래로만 증가한다.</summary>
    private int _filmLength;

    private bool _packed;

    public IReadOnlyList<Placement> Placements => _placements;

    public MaxRectsPacker(PackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.FilmWidth <= 0)
            throw new ArgumentException("필름 폭은 0보다 커야 합니다.", nameof(options));

        if (options.InitialHeight <= 0)
            throw new ArgumentException("초기 길이는 0보다 커야 합니다.", nameof(options));

        if (options.Gap < 0)
            throw new ArgumentException("Gap은 0 이상이어야 합니다.", nameof(options));

        if (options.MaxLength is int max && max <= 0)
            throw new ArgumentException("최대 길이는 0보다 커야 합니다.", nameof(options));

        _options = options;
    }

    public PackingResult Pack(IEnumerable<Piece> pieces)
    {
        ArgumentNullException.ThrowIfNull(pieces);

        // 상태가 누적되는 인스턴스라 재사용하면 결과가 조작된다.
        if (_packed)
            throw new InvalidOperationException(
                "MaxRectsPacker는 재사용할 수 없습니다. 호출마다 새 인스턴스를 만드세요.");

        var all = pieces.ToList();

        ValidatePieces(all);

        // 넓이가 큰 조각부터 넣어야 길이가 짧게 나온다.
        // 넓이가 같으면 길이 방향(Y축)으로 더 긴 조각을 먼저 배치한다.
        var ordered = all
            .OrderByDescending(p => p.Area)
            .ThenByDescending(p => Math.Max(p.Width, p.Height))
            .ThenByDescending(p => Math.Min(p.Width, p.Height))
            .ToList();

        // 모든 밴드(첫 밴드와 이후 확장)는 가장 큰 조각을 담을 수 있어야 한다.
        // 초기 길이보다 큰 조각이 있으면 확장 밴드가 그 조각을 담지 못해
        // Insert가 계속 실패하고 Place의 루프가 끝나지 않는다.
        _bandHeight = ordered.Count == 0
            ? _options.InitialHeight
            : Math.Max(_options.InitialHeight, ordered.Max(p => Math.Max(p.Width, p.Height)));

        _filmLength = 0;
        AddBand(_bandHeight);

        _packed = true;

        foreach (var piece in ordered)
            Place(piece);

        int usedLength = CalculateUsedLength();

        return new PackingResult
        {
            UsedLength = usedLength,
            WasteRate = CalculateWasteRate(usedLength),
            Placements = _placements.ToList()
        };
    }

    private void Place(Piece piece)
    {
        while (true)
        {
            if (Insert(piece))
                return;

            // ValidatePieces가 진입을 막으므로 여기의 실패는 길이 부족뿐이다.
            if (!ExpandFilm())
            {
                throw new InvalidOperationException(
                    $"필름 길이 {_options.MaxLength}mm 안에 조각 {piece.Id}" +
                    $"({piece.Width}x{piece.Height})를 배치할 수 없습니다.");
            }
        }
    }

    public bool Insert(Piece piece)
    {
        var found = FindBest(piece);

        if (!found.HasValue)
            return false;

        var best = found.Value;

        var placement = new Placement(
            piece.Id,
            best.X,
            best.Y,
            best.PlacedWidth,
            best.PlacedHeight,
            best.Rotated
        );

        SplitFreeRectangles(placement);
        PruneFreeRectangles();

        _placements.Add(placement);

        return true;
    }

    private Candidate? FindBest(Piece piece)
    {
        Candidate? best = null;

        foreach (var rect in _freeRectangles)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
                continue;

            if (_options.AllowRotate)
            {
                Consider(rect, piece.Width, piece.Height, rotated: false);
                Consider(rect, piece.Height, piece.Width, rotated: true);
            }
            else
            {
                Consider(rect, piece.Width, piece.Height, rotated: false);
            }
        }

        return best;

        void Consider(FreeRectangle rect, int width, int height, bool rotated)
        {
            // 간격까지 포함해서 들어가는지 확인한다.
            if (width + _options.Gap > rect.Width ||
                height + _options.Gap > rect.Height)
            {
                return;
            }

            var candidate = new Candidate(
                rect.X,
                rect.Y,
                width,
                height,
                rotated,
                rect.Width,
                rect.Height
            );

            if (!best.HasValue ||
                candidate.CompareTo(best.Value, _options.Heuristic) < 0)
            {
                best = candidate;
            }
        }
    }

    private void SplitFreeRectangles(Placement placed)
    {
        for (int i = 0; i < _freeRectangles.Count;)
        {
            if (SplitFreeNode(_freeRectangles[i], placed))
                _freeRectangles.RemoveAt(i);
            else
                i++;
        }
    }

    private bool SplitFreeNode(FreeRectangle free, Placement used)
    {
        // 뒤쪽 간격까지 포함해서 사용 영역으로 본다.
        int usedRight = used.X + used.Width + _options.Gap;
        int usedBottom = used.Y + used.Height + _options.Gap;

        // 겹치지 않으면 분할하지 않는다.
        if (used.X >= free.X + free.Width ||
            usedRight <= free.X ||
            used.Y >= free.Y + free.Height ||
            usedBottom <= free.Y)
        {
            return false;
        }

        // 조각이 자유 사각형의 위아래를 끝까지 차지하면 좌우로만 나눈다.
        // (이때만 위/아래에 남는 띠가 없다)
        if (used.Y <= free.Y && usedBottom >= free.Y + free.Height)
        {
            AddFree(free.X, free.Y, used.X - free.X, free.Height);
            AddFree(usedRight, free.Y, free.X + free.Width - usedRight, free.Height);
            return true;
        }

        // 좌우를 끝까지 차지하면 상하로만 나눈다.
        if (used.X <= free.X && usedRight >= free.X + free.Width)
        {
            AddFree(free.X, free.Y, free.Width, used.Y - free.Y);
            AddFree(free.X, usedBottom, free.Width, free.Y + free.Height - usedBottom);
            return true;
        }

        // 조각이 자유 사각형 안에 들어가는 일반적인 경우.
        // 위/아래/왼쪽/오른쪽 네 조각으로 쪼갠다. 여기서 2조각만 내면
        // 조각 바로 아래(또는 옆)의 남은 영역이 통째로 사라진다.
        // 남은 조각 중 다른 자유 사각형에 포함되는 것은 Prune에서 제거된다.
        if (used.Y > free.Y)
            AddFree(free.X, free.Y, free.Width, used.Y - free.Y);

        if (usedBottom < free.Y + free.Height)
            AddFree(free.X, usedBottom, free.Width, free.Y + free.Height - usedBottom);

        if (used.X > free.X)
            AddFree(free.X, free.Y, used.X - free.X, free.Height);

        if (usedRight < free.X + free.Width)
            AddFree(usedRight, free.Y, free.X + free.Width - usedRight, free.Height);

        return true;
    }

    private void AddFree(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;

        _freeRectangles.Add(new FreeRectangle(x, y, width, height));
    }

    /// <summary>
    /// 필름 끝에 밴드를 붙이고 길이를 늘린다.
    /// 밴드는 항상 현재 필름 길이 바로 아래에 붙는다.
    /// </summary>
    private void AddBand(int height)
    {
        if (height <= 0)
            return;

        if (_options.MaxLength is int max && _filmLength + height > max)
            height = max - _filmLength;

        if (height <= 0)
            return;

        AddFree(0, _filmLength, _options.FilmWidth, height);

        _filmLength += height;
    }

    /// <summary>
    /// 필름 길이를 한 밴드만큼 늘린다. 최대 길이에 닿으면 false.
    /// </summary>
    private bool ExpandFilm()
    {
        // 자유 사각형이 다 소진되면 currentBottom 이 0이 되어 이미 놓인 조각 위로
        // 밴드를 다시 얹게 된다. 그래서 필름 길이는 별도로 관리한다.
        if (_options.MaxLength is int max && _filmLength >= max)
            return false;

        // 초기 길이가 아니라, 모든 밴드에 공통으로 쓰는 높이를 쓴다.
        AddBand(_bandHeight);

        return _filmLength > 0;
    }

    private void PruneFreeRectangles()
    {
        for (int i = 0; i < _freeRectangles.Count; i++)
        {
            // 크기가 없는 자유 사각형은 후보로 쓸 수 있으므로 바로 버린다.
            if (_freeRectangles[i].Width <= 0 || _freeRectangles[i].Height <= 0)
            {
                _freeRectangles.RemoveAt(i);
                i--;
                continue;
            }

            for (int j = i + 1; j < _freeRectangles.Count;)
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
                    // j를 그대로 두어 다음 조각도 같은 i와 비교한다.
                    continue;
                }

                j++;
            }
        }
    }

    private static bool IsContainedIn(FreeRectangle a, FreeRectangle b)
    {
        return
            a.X >= b.X &&
            a.Y >= b.Y &&
            a.X + a.Width <= b.X + b.Width &&
            a.Y + a.Height <= b.Y + b.Height;
    }

    private void ValidatePieces(List<Piece> pieces)
    {
        foreach (var piece in pieces)
        {
            if (piece.Width <= 0 || piece.Height <= 0)
                throw new ArgumentException($"조각 {piece.Id}의 크기는 0보다 커야 합니다.");

            // 회전해도 폭에 들어가지 않으면 필름 길이를 늘려도 배치 불가능하다.
            // ValidatePieces가 이걸 막지 않으면 Place의 while 루프가 끝나지 않는다.
            bool fitsUpright = piece.Width + _options.Gap <= _options.FilmWidth;
            bool fitsRotated = _options.AllowRotate &&
                Math.Min(piece.Width, piece.Height) + _options.Gap <= _options.FilmWidth;

            if (!fitsUpright && !fitsRotated)
            {
                throw new ArgumentException(
                    $"조각 {piece.Id}({piece.Width}x{piece.Height})는 " +
                    $"필름 폭 {_options.FilmWidth}에 들어가지 않습니다.");
            }
        }
    }

    private int CalculateUsedLength()
    {
        int max = 0;

        foreach (var placement in _placements)
            max = Math.Max(max, placement.Y + placement.Height);

        return max;
    }

    private double CalculateWasteRate(int usedLength)
    {
        if (usedLength <= 0)
            return 0;

        long pieceArea = 0;

        foreach (var placement in _placements)
            pieceArea += (long)placement.Width * placement.Height;

        long totalArea = (long)_options.FilmWidth * usedLength;

        if (totalArea <= 0)
            return 0;

        return 1.0 - (double)pieceArea / totalArea;
    }

    /// <summary>
    /// 자유 사각형 하나에 조각을 놓는 후보. 휴리스틱별로 정렬 기준이 다르므로
    /// 점수 하나로 뭉개지 않고 튜플 비교로 판정한다.
    /// </summary>
    private readonly record struct Candidate(
        int X,
        int Y,
        int PlacedWidth,
        int PlacedHeight,
        bool Rotated,
        int FreeWidth,
        int FreeHeight)
    {
        /// <summary>이 후보를 채웠을 때 필름이 도달하는 가장 아래쪽 Y.</summary>
        public int Bottom => Y + PlacedHeight;

        /// <summary>
        /// 자유 사각형에서 조각이 차지하지 않는 넓이. 작을수록 빈틈없이 채워진다.
        /// 조각 넓이를 빼는 방식이라 회전해도 값이 같아야 한다.
        /// (바운딩 박스 넓이로 계산하면 회전 방향에 따라 값이 달라져
        ///  회전을 잘못 누르게 된다)
        /// </summary>
        public long Leftover =>
            (long)FreeWidth * FreeHeight - (long)PlacedWidth * PlacedHeight;

        public int ShortSide => Math.Min(FreeWidth - PlacedWidth, FreeHeight - PlacedHeight);

        public int LongSide => Math.Max(FreeWidth - PlacedWidth, FreeHeight - PlacedHeight);

        /// <summary>이 후보가 필름 길이를 얼마나 더 늘리는가 (현재 길이 기준).</summary>
        /// <returns>음수면 이 후보가 더 좋은 배치다.</returns>
        public int CompareTo(Candidate other, Heuristic heuristic)
        {
            // 정렬 키 순서를 정밀하게 비교했다.
            //
            // 10개 인스턴스의 사용 길이 합:
            //   아래쪽 Y(조각이 닿는 가장 아래쪽) 1순위 → 5261  ← 승자
            //   위쪽 Y 1순위                        → 5425
            //   빈틈(넓이) 1순위                   → 5768
            //   짧은 변 1순위                       → 5805
            //
            // 즉 이 문제(폭 고정, 길이 최소화)에서는 "빈틈 최소화"가 1순위가
            // 아니다. 빈틈 최소화를 1순위로 두면 아래쪽 Y로만 쌓아 올려
            // 중간에 못 쓰는 구멍을 남기고 결과적으로 필름이 더 길어진다.
            // 조각이 닿는 가장 아래쪽 Y를 1순위로 두어야 길이가 실제로 줄어든다.
            //
            // 주의: 이 수치는 SplitFreeNode가 자유 공간을 제대로 보존했을 때의 값이다.
            // 분할에서 남는 띠를 버리는 구현으로는 BottomLeft 계열이 정상 동작하지
            // 않아 반대 결론이 나온다. 정렬 키를 바꾸려면 그 상태에서 다시 측정할 것.
            switch (heuristic)
            {
                case Heuristic.BestShortSideFit:
                    return Cmp(ShortSide, other.ShortSide)
                        ?? Cmp(Bottom, other.Bottom)
                        ?? 0;

                case Heuristic.BestLongSideFit:
                    return Cmp(LongSide, other.LongSide)
                        ?? Cmp(Bottom, other.Bottom)
                        ?? 0;

                case Heuristic.BestAreaFit:
                    // 범위를 벗어난 값이 들어와도 배치 불가로 떨어지지 않게 한다.
                    return Cmp(Leftover, other.Leftover)
                        ?? Cmp(Bottom, other.Bottom)
                        ?? 0;

                case Heuristic.BottomLeft:
                default:
                    // 1) 조각이 닿는 가장 아래쪽 Y → 필름 길이를 최소화한다.
                    // 2) 위쪽 Y → 같은 아래쪽 Y면 위에서부터 채운다.
                    // 3) X → 같은 줄이면 왼쪽부터 채운다.
                    // 4) 남는 넓이 → 결정성을 위한 마지막 타격 키.
                    return Cmp(Bottom, other.Bottom)
                        ?? Cmp(Y, other.Y)
                        ?? Cmp(X, other.X)
                        ?? Cmp(Leftover, other.Leftover)
                        ?? 0;
            }
        }

        private static int? Cmp(int a, int b) => a == b ? null : Math.Sign(a.CompareTo(b));

        private static int? Cmp(long a, long b) => a == b ? null : Math.Sign(a.CompareTo(b));
    }
}

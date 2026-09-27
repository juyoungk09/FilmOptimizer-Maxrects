namespace FilmOptimizer.Core.Models;

using FilmOptimizer.Core.Algorithms.MaxRects;

public class PackingOptions
{
    /// <summary>
    /// 고정된 필름 폭 (X축). 이 값은 변하지 않는다.
    /// </summary>
    public int FilmWidth { get; init; }

    /// <summary>
    /// 처음 확보하는 필름 길이 (Y축). 부족하면 자동으로 늘어난다.
    /// </summary>
    public int InitialHeight { get; init; } = 500;

    /// <summary>
    /// 허용되는 최대 필름 길이 (Y축). 초과하면 최적화 실패.
    /// null이면 제한 없음.
    /// </summary>
    public int? MaxLength { get; init; }

    /// <summary>
    /// 조각 사이 최소 간격.
    /// </summary>
    public int Gap { get; init; } = 0;

    public bool AllowRotate { get; init; } = true;

    /// <summary>
    /// 아래쪽 Y를 1순위로 두는 정렬을 기본값으로 쓴다.
    /// 폭 고정·길이 최소화 목표에서 실측상 길이가 가장 짧았다.
    /// (MaxRectsPacker의 주석에 측정 표가 있다)
    /// </summary>
    public Heuristic Heuristic { get; init; } = Heuristic.BottomLeft;
}

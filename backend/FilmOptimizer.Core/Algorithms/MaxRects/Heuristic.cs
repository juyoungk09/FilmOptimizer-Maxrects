namespace FilmOptimizer.Core.Algorithms.MaxRects;

/// <summary>
/// 빈 사각형 안에서 조각을 놓을 위치를 고를 때의 비교 순서.
/// </summary>
public enum Heuristic
{
    BestShortSideFit = 0,

    BestLongSideFit = 1,

    BestAreaFit = 2,

    /// <summary>
    /// 아래쪽 Y 최소화가 1순위. 필름 길이(Y축)를 줄이는 데 가장 유리하다.
    /// 현재 기본값이다. (측정 근거는 MaxRectsPacker의 주석 참고)
    /// </summary>
    BottomLeft = 3
}

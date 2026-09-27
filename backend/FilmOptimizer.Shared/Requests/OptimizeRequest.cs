namespace FilmOptimizer.Shared.Requests;

public class OptimizeRequest
{
    /// <summary>고정된 필름 폭 (mm).</summary>
    public int FilmWidth { get; set; }

    /// <summary>조각 사이 간격 (mm).</summary>
    public int Gap { get; set; }

    public bool AllowRotate { get; set; }

    /// <summary>처음 확보할 필름 길이 (mm). 0이면 서버 기본값.</summary>
    public int InitialHeight { get; set; }

    /// <summary>허용할 최대 필름 길이 (mm). 0이면 제한 없음.</summary>
    public int MaxLength { get; set; }

    public List<PieceDto> Pieces { get; set; } = [];
}

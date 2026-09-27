namespace FilmOptimizer.Api.Services;

using FilmOptimizer.Core.Algorithms.MaxRects;
using FilmOptimizer.Core.Models;
using FilmOptimizer.Shared.Requests;
using FilmOptimizer.Shared.Responses;

public class PackingService : IPackingService
{
    public OptimizeResponse Optimize(OptimizeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FilmWidth <= 0)
            throw new ArgumentException("필름 폭은 0보다 커야 합니다.", nameof(request));

        if (request.Gap < 0)
            throw new ArgumentException("간격은 0 이상이어야 합니다.", nameof(request));

        if (request.InitialHeight < 0)
            throw new ArgumentException("초기 길이는 0 이상이어야 합니다.", nameof(request));

        if (request.MaxLength < 0)
            throw new ArgumentException("최대 길이는 0 이상이어야 합니다.", nameof(request));

        // 1. 옵션 생성
        // 길이 관련 값은 0이면 "지정 안 함"으로 보고 기본값/무제한을 쓴다.
        // 초기 길이는 비워 두면 패커가 가장 큰 조각에 맞춰 자동으로 올려준다.
        var options = new PackingOptions
        {
            FilmWidth = request.FilmWidth,
            InitialHeight = request.InitialHeight > 0 ? request.InitialHeight : 500,
            MaxLength = request.MaxLength > 0 ? request.MaxLength : null,
            Gap = request.Gap,
            AllowRotate = request.AllowRotate

            // Heuristic 는 지정하지 않는다. PackingOptions 의 기본값(BottomLeft)이
            // 실측상 길이가 가장 짧았다. 여기서 BestAreaFit 로 고정하면 그 이득이 사라진다.
        };

        // 2. DTO → Core Piece 변환
        var pieces = ConvertPieces(request.Pieces);

        // 3. 알고리즘 실행
        var packer = new MaxRectsPacker(options);

        var result = packer.Pack(pieces);

        // 4. Core → Response DTO
        return ConvertResult(result);
    }

    private static List<Piece> ConvertPieces(List<PieceDto> pieceDtos)
    {
        var pieces = new List<Piece>();

        int id = 1;

        foreach (var dto in pieceDtos)
        {
            for (int i = 0; i < dto.Count; i++)
            {
                pieces.Add(
                    new Piece(
                        id++,
                        dto.Width,
                        dto.Height
                    ));
            }
        }

        return pieces;
    }

    private static OptimizeResponse ConvertResult(PackingResult result)
    {
        return new OptimizeResponse
        {
            UsedLength = result.UsedLength,
            WasteRate = result.WasteRate,
            Placements = result.Placements
                .Select(p => new PlacementDto
                {
                    PieceId = p.PieceId,
                    X = p.X,
                    Y = p.Y,
                    Width = p.Width,
                    Height = p.Height,
                    Rotated = p.Rotated
                })
                .ToList()
        };
    }
}

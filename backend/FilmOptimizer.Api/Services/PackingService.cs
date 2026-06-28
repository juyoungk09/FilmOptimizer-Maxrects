namespace FilmOptimizer.Api.Services;
using FilmOptimizer.Core.Algorithms.MaxRects;
using FilmOptimizer.Core.Models;
using FilmOptimizer.Shared.Requests;
using FilmOptimizer.Shared.Responses;
public class PackingService : IPackingService
{
    public OptimizeResponse Optimize(OptimizeRequest request)
    {
        // 1. 옵션 생성
        var options = new PackingOptions
        {
            FilmWidth = request.FilmWidth,
            Gap = request.Gap,
            AllowRotate = request.AllowRotate
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
namespace FilmOptimizer.Tests;

using System.Text.Json;
using FilmOptimizer.Shared.Requests;
using Xunit;

/// <summary>
/// 프론트엔드 타입(frontend/src/types/Request.ts)과 C# DTO를 둘 다 손으로
/// 관리하고 있다. 한쪽만 고치면 조용히 0으로 바인딩되므로 여기서 고정한다.
/// </summary>
public class ContractTests
{
    /// <summary>ASP.NET Core MVC가 쓰는 기본 Json 옵션과 같은 설정.</summary>
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CamelCasePayload_BindsToOptimizeRequest()
    {
        // 프론트엔드가 실제로 보내는 JSON 그대로
        const string json = """
        {
          "filmWidth": 1200,
          "gap": 2,
          "allowRotate": true,
          "initialHeight": 600,
          "maxLength": 900,
          "pieces": [
            { "width": 81, "height": 152, "count": 2 },
            { "width": 31, "height": 40,  "count": 3 }
          ]
        }
        """;

        var request = JsonSerializer.Deserialize<OptimizeRequest>(json, Web);

        Assert.NotNull(request);
        Assert.Equal(1200, request.FilmWidth);
        Assert.Equal(2, request.Gap);
        Assert.True(request.AllowRotate);
        Assert.Equal(600, request.InitialHeight);
        Assert.Equal(900, request.MaxLength);
        Assert.Equal(2, request.Pieces.Count);
        Assert.Equal(81, request.Pieces[0].Width);
        Assert.Equal(3, request.Pieces[1].Count);
    }

    [Fact]
    public void MissingOptionalFields_FallBackToUnset()
    {
        // 기존 프론트엔드가 initialHeight/maxLength 없이 보내도 바인딩되어야 한다.
        const string json = """
        {
          "filmWidth": 800,
          "gap": 0,
          "allowRotate": false,
          "pieces": [{ "width": 100, "height": 50, "count": 1 }]
        }
        """;

        var request = JsonSerializer.Deserialize<OptimizeRequest>(json, Web);

        Assert.NotNull(request);
        Assert.Equal(800, request.FilmWidth);
        Assert.False(request.AllowRotate);

        // 서비스는 0을 "지정 안 함"으로 보고 기본값/무제한을 쓴다.
        Assert.Equal(0, request.InitialHeight);
        Assert.Equal(0, request.MaxLength);
        Assert.Single(request.Pieces);
    }
}

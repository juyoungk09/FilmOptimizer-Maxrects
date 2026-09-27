
using FilmOptimizer.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// CORS 는 "프론트와 API 를 따로 공개할 때" 를 위한 안전장치다.
// docker-compose 배포는 nginx 가 /api 를 같은 오리진으로 넘기므로 쓰이지 않는다.
// 프론트를 따로 도메인에 올리면 여기 목록에 그 Origin 을 추가한다.
// 환경변수로도 된다: Cors__AllowedOrigins__0=https://example.com
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Services
builder.Services.AddScoped<IPackingService, PackingService>();

// 컨테이너 상태 확인용. docker-compose 의 healthcheck 가 이 경로를 본다.
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors("Frontend");

// 배포 환경에 Swagger 를 그대로 노출하면 API 구조가 그대로 드러난고
// 스펙대로 요청해 볼 수 있다. 기본은 Development 뿐이고,
// 필요하면 Swagger__Enabled=true 로 켠다.
if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();

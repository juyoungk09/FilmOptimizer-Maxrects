# .NET 9 대상이다. 8.0 이미지를 쓰면 net9.0 어셈블리를 해석하지 못한다.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 프로젝트 참조 3개(csproj만 먼저 복사)를 먼저 복사한다.
# 코드와 무관한 layer 로 바뀌어 restore 가 캐시된다.
COPY ["FilmOptimizer.Api/FilmOptimizer.Api.csproj", "FilmOptimizer.Api/"]
COPY ["FilmOptimizer.Core/FilmOptimizer.Core.csproj", "FilmOptimizer.Core/"]
COPY ["FilmOptimizer.Shared/FilmOptimizer.Shared.csproj", "FilmOptimizer.Shared/"]
RUN dotnet restore "FilmOptimizer.Api/FilmOptimizer.Api.csproj"

COPY . .
RUN dotnet publish "FilmOptimizer.Api/FilmOptimizer.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# 상태 확인용. aspnet 이미지에는 curl 이 들어 있지 않다.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

# root 로 두지 않는다. APP_UID 는 이미지에 정의된 비특권 사용자다.
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "FilmOptimizer.Api.dll"]

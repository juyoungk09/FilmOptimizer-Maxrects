# AGENTS.md

필름 재단 최적화 서비스: MaxRects 빈 패킹 알고리즘을 감싼 .NET 9 API와 React 19 / Vite 8 / Konva 프론트엔드.

이 디렉터리가 git 저장소 루트다. 코드 주석과 UI 문자열은 한국어이고, 커밋 메시지는 영문 conventional-commit 접두사(`feat:`, `fix:`)를 쓴다.

## 구조

- `backend/FilmOptimizer.sln` — 프로젝트 4개. `FilmOptimizer.Api`(호스트/HTTP) → `Core` + `Shared`. `FilmOptimizer.Tests`는 `Core` + `Shared`만 참조하며 **`Api`는 참조하지 않는다**.
- `FilmOptimizer.Core/Algorithms/MaxRects/` — 알고리즘. `MaxRectsPacker.cs`만 실제로 구현되어 있고 `MaxRectsNode.cs`와 `MaxRectsUtility.cs`는 비어 있다.
- `FilmOptimizer.Shared/Requests|Responses` — HTTP 계약, 수동 작성.
- `frontend/src/types/{Request,Response}.ts` — 그 계약의 수동 중복 사본. 코드 생성도 OpenAPI 클라이언트도 없으니 양쪽을 항상 함께 수정해야 한다.
- 엔드포인트는 하나뿐: `POST /api/optimize` (`backend/FilmOptimizer.Api/Controllers/OptimizeControllers.cs:18`). `Program.cs`에서 Swagger를 개발 환경 조건 없이 항상 등록하므로 Production에서도 `/swagger`로 노출된다.

## 명령어

```bash
dotnet build backend/FilmOptimizer.sln
dotnet test  backend/FilmOptimizer.sln          # 20개: PackingTests(18) + ContractTests(2)
dotnet run --project backend/FilmOptimizer.Api  # http://localhost:5042, Swagger는 /swagger
```

```bash
cd frontend
npm run dev      # Vite 5173 포트 — 이 포트를 그대로 써야 한다. 아래 참조
npm run lint     # eslint .
npm run build    # tsc -b && vite build — 이것이 유일한 타입 체크
```

.NET 쪽 포매터/린터 설정은 없다. 프론트엔드 테스트 러너도 아예 없다.

테스트는 `PackingTests`(알고리즘 불변식: 폭 밖으로 나가지 않음, 조각끼리 안 겹침, 넓이 하한, 회전, 최대 길이 초과 시 예외, 그리고 `SplitFreeNode`/`Leftover`/밴드 확장 회귀)와 `ContractTests`(camelCase JSON ↔ C# DTO 바인딩)로 나뉘어 있다. 이 저장소에서 검증 수단이라 반드시 통과시켜야 한다.

배치가 눈으로 확인되지 않으면 임시 ASCII 시각화 테스트를 만들어 `%TEMP%`에 그림을 dumping 해서 볼 것. 겹침·맨 아래 빈 블록·과도한 길이 같은 문제가 한 번에 드러난다. (이런 임시 테스트 파일은 조사 후 반드시 지운다.)

## 배포 (docker compose)

```bash
docker compose up -d --build        # 빌드 + 기동
docker compose logs -f backend      # 로그
docker compose down                 # 정지
WEB_PORT=80 docker compose up -d    # 웹 포트 변경
```

브라우저는 `http://서버:8080`(또는 `WEB_PORT`)으로 접속한다. 개발은 로컬에서 `dotnet run` + `npm run dev`.

- **프론트(nginx)만 외부에 열린다.** 백엔드는 `expose` 뿐이고 호스트 포트 매핑이 없다. nginx 가 `/api` 를 같은 오리진으로 프록시하므로 브라우저 요청에 CORS 가 붙지 않는다.
- `frontend/nginx.conf` 가 `/api` → `http://backend:8080` 프록시, SPA 폴백(`try_files ... /index.html`), 캐시 정책(`/assets/` 1년 immutable, `index.html` 은 `no-store`)을 맡는다. index.html 을 오래 캐시하면 배포 후에도 옛 빌드가 보인다.
- **nginx 는 `listen 80` 과 `listen [::]:80` 를 모두 써야 한다.** 컨테이너 안에서 `localhost` 가 `::1` 로 잡히는데 IPv4 만 열면 연결이 거부된다. healthcheck 도 `localhost` 대신 `127.0.0.1` 을 쓴다.
- 프론트는 백엔드 주소를 하드코딩하지 않는다. `frontend/src/api/optimize.ts` 의 기본값은 `/api/optimize` 상대 경로이고 `VITE_API_URL` 로만 덮어쓴다. 개발은 `vite.config.ts` 의 프록시가 `/api` → `localhost:5042` 로 붙인다. 개발/배포가 같은 코드를 쓴다.
- 백엔드 CORS 허용 목록은 설정 `Cors:AllowedOrigins`(환경변수 `Cors__AllowedOrigins__0`)로 바꾼다. 기본은 `http://localhost:5173`. 같은 오리진으로 두면 CORS 는 쓰이지 않는다.
- **Swagger 는 Development 에서만 열린다.** 강제로 열려면 `Swagger__Enabled=true`. 이전에는 Production 에서도 `/swagger` 가 열려 있었다.
- `GET /health` 를 추가했고 compose healthcheck 가 이 경로를 본다. 백엔드 이미지에는 curl 이 없어 Dockerfile 에서 설치했고, 프론트는 `nginx:alpine` 의 busybox wget 로 확인한다.
- **`backend/.dockerignore` 는 필수다.** 없으면 빌드가 깨진다. `bin`/`obj` 가 저장소에 커밋돼 있어(추적 199개) Windows 빌드 산출물이 그대로 Linux 이미지에 복사된다. 같은 이유로 로컬에서 `dotnet build -p:BaseOutputPath=...` 로 우회하면 커밋된 `obj` 와 겹쳐 `CS0579` 중복 어셈블리 오류가 나므로 `-c Release` 로 빌드할 것.
- 이미지 크기: frontend(nginx) 약 74MB, backend(aspnet 9) 약 340MB.

## Cloudflare Tunnel

터널은 **호스트에서 이미 돌고 있으므로 이 compose 에 넣지 않는다.** `cloudflared` 를
컨테이너로 만들면 토큰 관리가 꼬이고, 기존 터널과 이중으로 뜨게 된다.

- 대시보드 → 터널 → Public Hostname → Service 를 `http://127.0.0.1:8080` 으로 지정한다.
  `WEB_PORT` 를 바꿨다면 그 포트와 맞춰야 한다.
- 그래서 프론트의 `ports:` 공개는 **유지해야 한다.** 터널이 컨테이너 안이 아니라
  호스트에서 돌기 때문에 이 포트가 입구다. 지우면 터널이 닿을 곳이 없다.
- 터널이 볼 origin 은 nginx 하나뿐이다. API 는 nginx 가 같은 오리진으로 프록시하므로
  터널 설정에 백엔드를 따로 추가할 필요가 없다.
- **CORS 는 손댈 필요 없다.** 확인 결과 Origin 이 허용 목록에 없어도 서버는 200 을 준다
  (브라우저는 크로스오리진일 때만 CORS 를 강제하는데, 이 경로는 같은 오리진이다).
  `Cors:AllowedOrigins` 는 프론트와 API 를 따로 도메인에 올릴 때만 손댄다.
- **`X-Forwarded-*` 처리(`UseForwardedHeaders`)는 넣지 않았다.** 이 앱은 `Request.Scheme`
  으로 HTTPS 리다이렉트도, 쿠키 `Secure` 도, 절대 URL 생성도 하지 않으므로 필요 없다.
  나중에 HTTPS 리다이렉트를 켜면 터널 뒤에서 리다이렉트 루프가 나는데, 그때 넣더라도
  **신뢰 프록시(`KnownProxies`/`KnownNetworks`)를 지정하지 않으면 클라이언트가
  `X-Forwarded-For` 를 위조할 수 있다.**
- Cloudflare 무료 요금제 요청 본문 한계는 100MB 다. nginx 는 `client_max_body_size 10m`
  으로 더 작게 두었다. 기본값 1MB 는 조각 2만 7천 개쯤에서 413 이 나서 반드시 명시해야
  한다. 413 은 HTML 이 아니라 JSON `{"message":...}` 으로 돌려줘서 사이드바에 그대로 뜬다.

## 이 환경에서 docker 를 못 쓸 때

- WSL 배포판의 docker 래퍼가 **WSL 연동이 꺼져 있으면 명령을 아예 거부한다**(The command 'docker' could not be found in this WSL 2 distro). Docker Desktop 이 켜져 있어도 이게 난다. Docker Desktop 설정 → Resources → WSL Integration 에서 해당 배포판을 켜야 하며 GUI 작업이라 에이전트가 못 한다.
- 우회: Windows 엔진에 직접 요청한다. Docker Desktop 을 먼저 실행시키고 아래 경로를 쓴다.
  `"/mnt/c/Program Files/Docker/Docker/resources/bin/docker.exe" compose up -d --build`

## 프론트엔드 ↔ 백엔드 연결

- API URL은 `frontend/src/api/optimize.ts`의 `API_URL` 상수에 있다. 기본값은 `/api/optimize` **상대 경로**이고 `VITE_API_URL` 로만 덮어쓴다. 개발은 `vite.config.ts` 의 프록시가 `/api` → `http://localhost:5042` 로 붙이고, 배포는 nginx 가 `/api` → `backend:8080` 로 붙인다.
- 로컬 개발은 백엔드를 5042(`launchSettings.json`), Vite 를 5173 으로 띄운다. compose 는 프론트만 `WEB_PORT`(기본 8080) 에 열고 백엔드는 열지 않는다.
- CORS 허용 목록은 `Cors:AllowedOrigins` 설정으로 덮어쓰며 기본은 `http://localhost:5173` 이다(`Program.cs`). 같은 오리진(nginx 프록시) 경로에서는 CORS 가 붙지 않으니 배포 시에는 쓰이지 않는다.
- 조각 DTO 는 `{ width, height, count }` 다 — `id` 가 없고 `count` 가 반복 횟수다. `count` 를 빼면 조각이 0개가 되어 아무것도 배치되지 않는다.
- 컨트롤러는 `ArgumentException`을 400, `InvalidOperationException`(최대 길이 초과)을 422로 돌려준다. `api/optimize.ts`가 `response.data.message`를 꺼내 `Error`로 다시 던지고, 사이드바가 그 메시지를 그대로 보여준다. 서버 메시지를 바꾸면 이 경로도 같이 확인한다.
- `PreviewCanvas`는 컨테이너 크기를 `ResizeObserver`로 재서 자동 확대한다. `filmWidth`는 반드시 props로 받아야 한다(예전엔 1200을 하드코딩해서 사이드바 입력을 무시했다).

## 검증

- 이 환경에는 `dotnet`/`node`가 PATH에 없다. 대신 Windows SDK가 WSL 인터옵으로 닿는다:
  `"/mnt/c/Program Files/dotnet/dotnet.exe" test backend/FilmOptimizer.Tests/FilmOptimizer.Tests.csproj`
  빌드·테스트는 **항상 이 경로로** 한다. 실행 중인 `FilmOptimizer.Api`가 DLL을 잠근 상태라 실패하면(`MSB3021/MSB3027`) 잠근 프로세스를 죽이거나 `-p:BaseOutputPath=<임의>/` 로 우회한다.
- 이 머신에는 **NuGet 소스가 하나도 설정돼 있지 않다**(`dotnet nuget list source` → "찾을 수 없습니다"). 그래서 테스트 프로젝트가 `NU1100`으로 복원 실패한다. 복원할 때 소스를 명시한다: `--source https://api.nuget.org/v3/index.json`. 앱 프로젝트는 패키지가 이미 캐시돼 있어 이 영향을 안 받는다.
- WSL에서 띄운 서버는 Windows `localhost`에 닿지 않고(방화벽), 역도 성립하지 않는다. `netstat`/`curl`이 전부 000을 준다. **소켓을 직접 확인하려 하지 말고 테스트로 검증한다.** 계약은 `ContractTests`가 camelCase 바인딩을 직접 확인한다.
- `dotnet test`에 `--blame-hang-timeout 90s`를 붙여서 무한 루프를 90초에 끊는다(실제로 한 번 걸린 적이 있다).


## 알고리즘 관련

목표는 **폭(X축) 고정, 길이(Y축) 최소화**다. `X`는 필름 폭, `Y`는 필름을 따라 내려가는 방향이다.

- **`MaxRectsPacker`는 상태가 누적된다.** `_freeRectangles`와 `_placements`가 인스턴스 필드이고 `Pack`/`Insert`가 직접 바꾼다. `Pack`을 두 번 부르면 `InvalidOperationException`이 난다. **호출마다 새 packer를 만들어야 한다** — `PackingService`는 이걸 지키고 있다.
- **밴드 높이는 가장 큰 조각 이상이어야 한다** (`_bandHeight`). 초기 길이보다 긴 조각이 있으면 확장 밴드가 그 조각을 담지 못해 `Place`의 `while` 루프가 끝나지 않는다. 초기 밴드만 크게 잡고 확장 밴드를 `InitialHeight`로 두면 이 버그가 그대로 돌아온다.
- **밴드는 반드시 필름 끝에 붙여야 한다** (`_filmLength`). 이전 구현은 자유 사각형들의 최대 하단을 밴드 시작 Y로 썼는데, 자유 사각형이 하나도 안 남으면 그 값이 0이 되어 이미 놓인 조각 **위에** 밴드를 다시 얹게 된다(조각이 겹치고 폐기율이 음수가 된다). 필름 길이는 `_filmLength` 필드로 따로 증가시켜야 한다.
- **`SplitFreeNode`에서 남는 띠를 버리지 말 것.** 조각이 자유 사각형 안에 완전히 들어가면 위/아래/왼쪽/오른쪽 **네 조각**으로 쪼개야 한다. 좌우(또는 상하) 두 조각만 내면 조각 바로 아래·옆의 남은 공간이 통째로 사라진다. 좌우로만 쪼갤 수 있는 예외는 "조각이 자유 사각형의 위아래를 끝까지 차지한" 경우뿐이다(가로도 동일). 이 버그가 있으면 폭 400 조각 14개가 392 가 아니라 **2060** 까지 늘어난다.
- **정렬 키를 바꾸려면 반드시 다시 측정할 것.** 실측(5개 인스턴스 사용 길이 합):
  - 조각이 닿는 가장 아래쪽 Y 1순위(`BottomLeft`, **현재 기본값**) → **2752**
  - 위쪽 Y 1순위 → 3025
  - 빈틈(넓이) 1순위 → 3199
  - 짧은 변 1순위 → 3404

  즉 이 문제(폭 고정·길이 최소화)에서는 **아래쪽 Y 최소화가 1순위**여야 필름이 실제로 짧아진다. 빈틈 최소화를 1순위로 두면 위쪽 빈틈부터 메우다 중간에 못 쓰는 구멍을 남겨 결과적으로 더 길어진다. 수치는 `PackingTests.DefaultHeuristic_MatchesTheMeasuredBestTotal`에 고정돼 있다.
  - **주의: 위 수치는 `SplitFreeNode`가 제대로 동작할 때의 값이다.** 분할이 남는 띠를 버리던时期에는 BottomLeft 계열이 정상 동작하지 않아 반대 결론(아래쪽 Y가 13% 더 나쁘다)이 나왔다. 그 잘못된 결론이 한때 `AGENTS.md`에 적혀 있었다. 정렬 키 비교는 항상 "분할이 올바른 상태"에서 다시 돌릴 것.
- **`Heuristic`의 기본값은 `BottomLeft`**(Bottom → Y → X → Leftover 순)이고 `PackingOptions.Heuristic`의 초기값이다. `PackingService`는 이 값을 지정하지 않는다(명시적으로 `BestAreaFit`으로 고정하면 실측 이득이 사라진다). 알 수 없는 enum 값이 들어와도 배치 불가로 떨어지지 않게 `default`가 `BottomLeft`로 처리된다.
- **`Leftover`는 회전 불변이어야 한다.** `자유사각형넓이 - 조각넓이`로 계산한다. `(FreeWidth - PlacedWidth) * (FreeHeight - PlacedHeight)`처럼 바운딩 박스 넓이로 계산하면 조각 넓이가 같아도 회전 방향에 따라 값이 달라져 잘못된 방향을 고른다(프론트 기본 입력에서 81 → 230 으로 악화).
- `InitialHeight`은 배치 품질에도 영향을 준다. 밴드 높이가 크면 확장이 늦어져 자유 사각형이 넉넉해진다(같은 입력 14개 조각: `InitialHeight=500` → 392, `200` → 472). 기본값은 500이다.
- 회전해도 필름 폭에 들어가지 않는 조각은 `ValidatePieces`가 `ArgumentException`으로 거절한다. 이 검사가 없으면 길이를 아무리 늘려도 배치가 불가능해 루프가 멈추지 않는다.
- `PackingOptions.MaxLength`(nullable)을 주면 길이 초과 시 `InvalidOperationException`으로 멈춘다. 지정하지 않으면 무제한이므로 무한 확장 위험이 있다.
- `OptimizeRequest`의 `initialHeight`/`maxLength`는 **0이면 "지정 안 함"**이고 `PackingService`가 기본값(500)/무제한으로 바꾼다.
- Gap은 적합성 검사와 분할에만 들어가고, 최종 `Placement`의 width/height는 조각 원래 크기다.


## Git 위생

- 루트 `.gitignore`가 **없고**, `backend/**/bin`과 `obj`는 이미 **커밋되어 있다**(추적 파일 262개 중 약 199개). `git status`는 항상 지저분하므로 절대 `git add -A`를 쓰지 말 것.
- `.gitattributes`가 없고 `core.autocrlf`가 설정되지 않았는데, 워킹 트리는 CRLF이고 HEAD는 LF다. `docker-compose.yml`, `backend/Dockerfile`, `FilmOptimizer.sln`을 Windows 도구로 편집하면 파일 전체가 공백만 바뀌는 diff가 생긴다. 커밋 전에 `git diff --stat`으로 확인할 것.
- `FilmOptimizer.sln`은 UTF-8 BOM으로 추적 중이다. 유지할 것.

## 관례

- 테스트는 packer 스모크 테스트 하나뿐이다. `backend/FilmOptimizer.Tests`는 `Core`와 `Shared`만 참조하므로 API/컨트롤러 테스트가 필요하면 `FilmOptimizer.Api`로 `ProjectReference`를 새로 추가해야 한다. `ContractTests`가 대신 JSON 계약만 검증한다.
- `frontend/tsconfig.app.json`에 `strict`가 없다. 표준 Vite React-TS 템플릿이 잡아낼 타입 오류가 여기는 통과한다.
- `new Error(...)`를 던질 때 `cause`를 붙이지 않으면 eslint `preserve-caught-error`가 실패한다.
- 죽은 스캐폴딩은 요청 없이 건드리지 말 것: `FilmOptimizer.Core/Class1.cs`, `Geometry/Rect.cs`, `Models/Film.cs`, 빈 `MaxRectsNode.cs`/`MaxRectsUtility.cs`, 미사용 의존성(`@tanstack/react-query`, `zod`, `react-hook-form`, `clsx`, `lucide-react`). `FilmOptimizer.Api.http`는 여전히 존재하지 않는 `/weatherforecast/`를 요청한다.

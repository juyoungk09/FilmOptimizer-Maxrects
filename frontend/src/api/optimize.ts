import axios from "axios";
import type { OptimizeRequest } from "../types/Request";
import type { OptimizeResponse } from "../types/Response";

// 같은 오리진 상대 경로가 기본이다.
//   배포: nginx 가 /api 를 백엔드로 넘긴다 (docker-compose)
//   개발: Vite 프록시가 /api 를 localhost:5042 로 넘긴다 (vite.config.ts)
// 어느 쪽이든 프론트 코드는 같은 경로를 쓴다.
// 백엔드를 직접 붙잡아야 할 때만 VITE_API_URL 로 절대 주소를 넣는다.
const API_URL = import.meta.env.VITE_API_URL || "/api/optimize";

export async function optimize(request: OptimizeRequest) {
    try {
        const res = await axios.post<OptimizeResponse>(API_URL, request);

        return res.data;
    } catch (err) {
        // 서버가 알려준 이유(잘못된 입력, 최대 길이 초과 등)를 그대로 넘긴다.
        if (axios.isAxiosError<{ message?: string }>(err)) {
            throw new Error(
                err.response?.data?.message ?? "최적화 요청에 실패했습니다.",
                { cause: err }
            );
        }

        throw new Error("최적화 요청에 실패했습니다.", { cause: err });
    }
}

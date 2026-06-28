import axios from "axios";
import type { OptimizeRequest } from "../types/Request";
import type { OptimizeResponse } from "../types/Response";

export async function optimize(request: OptimizeRequest) {

    const res = await axios.post<OptimizeResponse>(
        "http://localhost:5042/api/optimize",
        request
    );

    return res.data;
}
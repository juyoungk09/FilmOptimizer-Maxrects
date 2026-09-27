export interface OptimizeRequest {
    /** 고정된 필름 폭 (mm) */
    filmWidth: number;
    /** 조각 사이 간격 (mm) */
    gap: number;
    allowRotate: boolean;
    /** 처음 확보할 필름 길이 (mm). 0이면 서버가 정한다. */
    initialHeight?: number;
    /** 허용할 최대 필름 길이 (mm). 0이면 제한 없음. */
    maxLength?: number;

    pieces: {
        width: number;
        height: number;
        count: number;
    }[];
}

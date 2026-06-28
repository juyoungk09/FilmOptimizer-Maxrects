export interface OptimizeRequest {
    filmWidth: number;
    gap: number;
    allowRotate: boolean;

    pieces: {
        width: number;
        height: number;
        count: number;
    }[];
}
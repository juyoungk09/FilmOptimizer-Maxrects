import { Fragment, useLayoutEffect, useRef, useState } from "react";
import { Layer, Line, Rect, Stage, Text } from "react-konva";
import type { OptimizeResponse } from "../../types/Response";

interface Props {
    result: OptimizeResponse | null;
    filmWidth: number;
}

/** 캔버스 바깥 여백 */
const PADDING = 24;

/** 왼쪽 길이 눈금이 차지하는 자리 */
const RULER = 40;

/** 자동 확대 배율의 상한/하한 */
const MAX_SCALE = 8;
const MIN_SCALE = 0.02;

/** 이보다 작으면 번호 라벨을 그리지 않는다 */
const LABEL_MIN_WIDTH = 22;
const LABEL_MIN_HEIGHT = 18;

const PIECE_FILL = [
    "#60a5fa",
    "#34d399",
    "#fbbf24",
    "#f472b6",
    "#a78bfa",
    "#22d3ee",
    "#fb923c",
    "#4ade80",
];

/**
 * 필름 미리보기.
 *
 * 폭은 고정이고 길이(Y축)만 늘어나므로 세로로 긴 띠로 그린다.
 * 사용 길이보다 아래는 아예 그리지 않아 빈 공간을 만들지 않고,
 * 컨테이너 크기에 맞춰 확대 비율을 계산해 화면을 최대한 채운다.
 */
export default function PreviewCanvas({ result, filmWidth }: Props) {
    const containerRef = useRef<HTMLDivElement>(null);
    const [size, setSize] = useState({ width: 0, height: 0 });

    useLayoutEffect(() => {
        const el = containerRef.current;

        if (!el)
            return;

        const observer = new ResizeObserver((entries) => {
            const rect = entries[0].contentRect;

            setSize({ width: rect.width, height: rect.height });
        });

        observer.observe(el);

        return () => observer.disconnect();
    }, []);

    const usedLength = result?.usedLength ?? 0;
    const hasResult = filmWidth > 0 && usedLength > 0;

    const scale = fitScale(size.width, size.height, filmWidth, usedLength);

    const filmW = filmWidth * scale;
    const filmH = usedLength * scale;

    // 눈금 자리와 여백을 제외한 실제 그리기 영역
    const areaX = RULER + PADDING;
    const areaY = PADDING;
    const areaW = Math.max(size.width - areaX - PADDING, 0);
    const areaH = Math.max(size.height - areaY - PADDING * 2, 0);

    // 영역 안에 가운데 정렬
    const originX = areaX + Math.max((areaW - filmW) / 2, 0);
    const originY = areaY + Math.max((areaH - filmH) / 2, 0);

    const step = niceStep(usedLength, 8);
    const ticks: number[] = [];

    if (hasResult && step > 0) {
        for (let value = step; value < usedLength; value += step)
            ticks.push(value);
    }

    return (
        <section className="flex min-w-0 flex-1 flex-col">
            <div className="flex items-center justify-between border-b bg-white px-6 py-3">
                <h2 className="font-semibold">
                    미리보기
                </h2>

                {hasResult && (
                    <div className="flex gap-6 text-sm text-slate-500">
                        <span>
                            폭 {filmWidth} mm
                        </span>
                        <span>
                            길이 {usedLength} mm
                        </span>
                        <span>
                            확대 {scale.toFixed(2)}배
                        </span>
                    </div>
                )}
            </div>

            <div
                ref={containerRef}
                className="min-h-0 flex-1 overflow-hidden bg-slate-200"
            >
                <Stage
                    width={size.width}
                    height={size.height}
                >
                    <Layer>
                        {!hasResult && (
                            <Text
                                x={0}
                                y={size.height / 2 - 10}
                                width={size.width}
                                align="center"
                                fontSize={14}
                                fill="#64748b"
                                text="왼쪽에서 조각을 입력하고 최적화를 누르면 결과가 표시됩니다."
                            />
                        )}

                        {hasResult && (
                            <>
                                {/* 필름 본체: 사용 길이까지만 그린다 */}
                                <Rect
                                    x={originX}
                                    y={originY}
                                    width={filmW}
                                    height={filmH}
                                    fill="#ffffff"
                                    stroke="#0f172a"
                                    strokeWidth={1.5}
                                />

                                {/* 길이 눈금 */}
                                {ticks.map((value) => {
                                    const y = originY + value * scale;

                                    return (
                                        <Fragment key={value}>
                                            <Line
                                                points={[
                                                    originX - 6,
                                                    y,
                                                    originX,
                                                    y,
                                                ]}
                                                stroke="#94a3b8"
                                                strokeWidth={1}
                                            />
                                            <Text
                                                x={0}
                                                y={y - 6}
                                                width={RULER}
                                                align="right"
                                                fontSize={11}
                                                fill="#64748b"
                                                text={String(value)}
                                            />
                                        </Fragment>
                                    );
                                })}

                                {/* 조각 */}
                                {result!.placements.map((piece, index) => {
                                    const x = originX + piece.x * scale;
                                    const y = originY + piece.y * scale;
                                    const w = piece.width * scale;
                                    const h = piece.height * scale;

                                    // 인접한 조각이 붙어 보이도록 1px 안쪽으로 그린다
                                    const showLabel =
                                        w >= LABEL_MIN_WIDTH && h >= LABEL_MIN_HEIGHT;

                                    return (
                                        <Fragment key={piece.pieceId}>
                                            <Rect
                                                x={x + 0.5}
                                                y={y + 0.5}
                                                width={Math.max(w - 1, 0)}
                                                height={Math.max(h - 1, 0)}
                                                fill={
                                                    PIECE_FILL[
                                                    index % PIECE_FILL.length
                                                    ]
                                                }
                                                stroke="#0f172a"
                                                strokeWidth={0.5}
                                            />
                                            {showLabel && (
                                                <Text
                                                    x={x + 4}
                                                    y={y + 2}
                                                    fontSize={Math.min(12, h - 4)}
                                                    fill="#0f172a"
                                                    text={String(piece.pieceId)}
                                                />
                                            )}
                                        </Fragment>
                                    );
                                })}
                            </>
                        )}
                    </Layer>
                </Stage>
            </div>
        </section>
    );
}

/** 사용 길이 전체가 보이도록 확대 비율을 정한다. */
function fitScale(
    availW: number,
    availH: number,
    contentW: number,
    contentH: number
) {
    if (availW <= 0 || availH <= 0 || contentW <= 0 || contentH <= 0)
        return 0;

    const usableW = Math.max(availW - (RULER + PADDING) - PADDING, 1);
    const usableH = Math.max(availH - PADDING * 2, 1);

    const scale = Math.min(usableW / contentW, usableH / contentH);

    return Math.min(Math.max(scale, MIN_SCALE), MAX_SCALE);
}

/** 눈금 간격을 1/2/5 × 10ⁿ 같은 깔끔한 값으로 잡는다. */
function niceStep(length: number, targetTicks: number) {
    if (length <= 0 || targetTicks <= 0)
        return 0;

    const raw = length / targetTicks;
    const magnitude = 10 ** Math.floor(Math.log10(raw));
    const normalized = raw / magnitude;

    const step =
        normalized <= 1 ? 1 : normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10;

    return step * magnitude;
}

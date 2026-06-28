import { Stage, Layer, Rect, Text } from "react-konva";
import type { OptimizeResponse } from "../../types/Response";

interface Props {
    result: OptimizeResponse | null;
}

const SCALE = 0.5; // 1mm = 0.5px

export default function PreviewCanvas({ result }: Props) {
    const filmWidth = 1200;

    const usedLength = result?.usedLength ?? 500;

    return (
        <section className="flex flex-1 flex-col">

            <div className="border-b bg-white px-6 py-3">
                <h2 className="font-semibold">
                    미리보기
                </h2>
            </div>

            <div className="flex flex-1 items-center justify-center overflow-auto bg-slate-200">

                <Stage
                    width={filmWidth * SCALE + 60}
                    height={usedLength * SCALE + 60}
                >
                    <Layer>

                        {/* 필름 */}

                        <Rect
                            x={30}
                            y={30}
                            width={filmWidth * SCALE}
                            height={usedLength * SCALE}
                            stroke="black"
                            strokeWidth={2}
                            fill="#fafafa"
                        />

                        {/* 조각 */}

                        {result?.placements.map((piece) => (

                            <Rect
                                key={piece.pieceId}

                                x={30 + piece.x * SCALE}
                                y={30 + piece.y * SCALE}

                                width={piece.width * SCALE}
                                height={piece.height * SCALE}

                                fill="#60a5fa"

                                stroke="black"

                                strokeWidth={1}
                            />

                        ))}

                        {/* 번호 */}

                        {result?.placements.map((piece) => (

                            <Text

                                key={"text" + piece.pieceId}

                                x={35 + piece.x * SCALE}

                                y={35 + piece.y * SCALE}

                                text={piece.pieceId.toString()}

                                fontSize={14}

                            />

                        ))}

                    </Layer>
                </Stage>

            </div>

        </section>
    );
}
import type { Piece } from "../../types/Piece";
import { optimize } from "../../api/optimize";
import type { OptimizeResponse } from "../../types/Response";

interface Props {

    filmWidth:number;   
    setFilmWidth:(v:number)=>void;

    gap:number;
    setGap:(v:number)=>void;

    allowRotate:boolean;
    setAllowRotate:(v:boolean)=>void;

    pieces:Piece[];
    setPieces:(p:Piece[])=>void;

    setResult:(r:OptimizeResponse)=>void;

}

export default function Sidebar({
    filmWidth,
    setFilmWidth,

    gap,
    setGap,

    allowRotate,
    setAllowRotate,

    pieces,
    setPieces,

    setResult

}:Props){

    async function handleOptimize() {
        try {
            const request = {
                filmWidth,
                gap,
                allowRotate,
                pieces
            };
            const result = await optimize(request);
            setResult(result);
        }
        catch (err) {
            console.error(err);
            alert("최적화 실패");
        }
    }
    function addPiece() {
        setPieces([
            ...pieces,
            {
                width: 0,
                height: 0,
                count: 1,
            },
        ]);
    }
    function removePiece(index: number) {
        setPieces(
            pieces.filter((_, i) => i !== index)
        );
    }
    function updatePiece(
        index: number,
        key: keyof Piece,
        value: number
    ) {
        const copy = [...pieces];
        copy[index][key] = value;
        setPieces(copy);
    }
    return (
        <aside className="w-96 overflow-auto border-r bg-white p-6">
            <h2 className="mb-6 text-xl font-bold">
                입력
            </h2>
            <div className="space-y-4">
                <div>
                    <label className="mb-1 block text-sm">
                        필름 폭
                    </label>
                    <input
                        type="number"
                        value={filmWidth}
                        onChange={(e) => setFilmWidth(Number(e.target.value))}
                        className="w-full rounded border p-2"
                    />
                </div>
                <div>
                    <label className="mb-1 block text-sm">
                        Gap
                    </label>
                    <input
                        type="number"
                        value={gap}
                        onChange={(e) => setGap(Number(e.target.value))}
                        className="w-full rounded border p-2"
                    />
                </div>
                <label className="flex items-center gap-2">
                    <input
                        type="checkbox"
                        checked={allowRotate}
                        onChange={(e) => setAllowRotate(e.target.checked)}
                    />
                    회전 허용
                </label>
                <hr />
                <h3 className="font-semibold">
                    조각 목록
                </h3>
                {
                    pieces.map((piece, index) => (
                        <div key={index} className="flex gap-2">
                            <input
                                type="number"
                                value={piece.width}
                                onChange={(e) => updatePiece(index, "width", Number(e.target.value))}
                                className="w-20 rounded border p-2"
                            />
                            <input
                                type="number"
                                value={piece.height}
                                onChange={(e) => updatePiece(index, "height", Number(e.target.value))}
                                className="w-20 rounded border p-2"
                            />
                            <input
                                type="number"
                                value={piece.count}
                                onChange={(e) => updatePiece(index, "count", Number(e.target.value))}
                                className="w-16 rounded border p-2"
                            />
                            <button
                                onClick={() => removePiece(index)}
                                className="rounded bg-red-500 px-3 text-white"
                            >
                                X
                            </button>
                        </div>
                    ))
                }
                <button
                    onClick={addPiece}
                    className="w-full rounded bg-gray-200 py-2"
                >
                    + 조각 추가
                </button>
                <button
                    onClick={handleOptimize}
                    className="w-full rounded bg-blue-600 py-3 font-semibold text-white hover:bg-blue-700"
                >
                    최적화
                </button>
            </div>
        </aside>
    );
}
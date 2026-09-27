import { useState, type ChangeEvent } from "react";

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

    const [error, setError] = useState<string | null>(null);

    async function handleOptimize() {
        setError(null);

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
            setError(err instanceof Error ? err.message : "최적화 실패");
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
        <aside className="w-full shrink-0 border-b bg-white p-4 sm:p-6 lg:w-96 lg:overflow-y-auto lg:border-r lg:border-b-0">
            <h2 className="mb-6 text-lg font-bold sm:text-xl">
                입력
            </h2>
            <div className="space-y-4">
                <div>
                    <label className="mb-1 block text-sm">
                        필름 폭
                    </label>
                    <NumberInput
                        label="필름 폭"
                        value={filmWidth}
                        onChange={setFilmWidth}
                        className="w-full min-w-0 rounded border p-2"
                    />
                </div>
                <div>
                    <label className="mb-1 block text-sm">
                        Gap
                    </label>
                    <NumberInput
                        label="Gap"
                        value={gap}
                        onChange={setGap}
                        className="w-full min-w-0 rounded border p-2"
                    />
                </div>
                <label className="flex min-h-11 items-center gap-2">
                    <input
                        type="checkbox"
                        className="size-4"
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
                        <div
                            key={index}
                            className="grid grid-cols-[1fr_1fr_1fr_auto] items-center gap-2"
                        >
                            <NumberInput
                                label="조각 폭"
                                value={piece.width}
                                onChange={(v) => updatePiece(index, "width", v)}
                                className="w-full min-w-0 rounded border p-2"
                            />
                            <NumberInput
                                label="조각 높이"
                                value={piece.height}
                                onChange={(v) => updatePiece(index, "height", v)}
                                className="w-full min-w-0 rounded border p-2"
                            />
                            <NumberInput
                                label="조각 개수"
                                value={piece.count}
                                onChange={(v) => updatePiece(index, "count", v)}
                                className="w-full min-w-0 rounded border p-2"
                            />
                            <button
                                onClick={() => removePiece(index)}
                                aria-label="조각 삭제"
                                className="min-h-11 min-w-11 rounded bg-red-500 px-3 text-white"
                            >
                                X
                            </button>
                        </div>
                    ))
                }
                <button
                    onClick={addPiece}
                    className="min-h-11 w-full rounded bg-gray-200 py-2"
                >
                    + 조각 추가
                </button>
                <button
                    onClick={handleOptimize}
                    className="min-h-11 w-full rounded bg-blue-600 py-3 font-semibold text-white hover:bg-blue-700"
                >
                    최적화
                </button>

                {
                    error && (

                        <div className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                            {error}
                        </div>

                    )
                }
            </div>
        </aside>
    );
}

interface NumberInputProps {

    value:number;
    onChange:(v:number)=>void;

    /** 스크린리더용 이름 */
    label?:string;

    className?:string;

}

/**
 * 숫자 입력.
 *
 * 비어 있는 중간 상태를 허용한다. 곧바로 `Number(...)` 로 올리면 빈 문자열이 0 이 되어
 * 입력창에 0 이 다시 찍히고, 그래서 0 을 지울 수 없게 된다.
 *
 * 실제로 숫자로 읽히는 값만 부모에 알리고, 포커스를 벗어나면 빈칸을 0 으로 채운다.
 * 0 은 이 화면에서 정상값이다(gap 0, 개수 0) 그래서 `required` 대신 이 규칙을 쓴다.
 */
function NumberInput({
    value,
    onChange,

    label,
    className
}:NumberInputProps){

    const [text, setText] = useState(() => String(value));
    const [pushed, setPushed] = useState(value);

    // 바깥에서 값이 바뀌면 문자열을 맞춰준다.
    // 조각을 지울 때 `key` 가 인덱스라 자리가 바뀐 조각도 이 경로로 갱신된다.
    if (value !== pushed) {
        setPushed(value);
        setText(String(value));
    }

    function commit(next:number) {
        setPushed(next);
        onChange(next);
    }

    function handleChange(e:ChangeEvent<HTMLInputElement>) {
        const raw = e.target.value;

        setText(raw);

        const parsed = Number(raw);

        // 빈칸이나 아직 입력 중인 값은 부모에 올리지 않는다
        if (raw.trim() === "" || !Number.isFinite(parsed))
            return;

        commit(parsed);
    }

    function handleBlur() {

        // 포커스를 벗어나면 빈칸을 0 으로 확정한다
        if (text.trim() === "" || !Number.isFinite(Number(text))) {
            setText("0");
            commit(0);
        }
    }

    return (
        <input
            type="number"
            inputMode="numeric"
            aria-label={label}
            value={text}
            onChange={handleChange}
            onBlur={handleBlur}
            className={className}
        />
    );
}

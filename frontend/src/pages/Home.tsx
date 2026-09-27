import { useState } from "react";

import Header from "../components/layouts/Header";
import Sidebar from "../components/layouts/Sidebar";
import PreviewCanvas from "../components/canvas/PreviewCanvas";
import Footer from "../components/layouts/Footer";

import type { Piece } from "../types/Piece";
import type { OptimizeResponse } from "../types/Response";

export default function Home() {

    const [filmWidth, setFilmWidth] = useState(1200);

    const [gap, setGap] = useState(2);

    const [allowRotate, setAllowRotate] = useState(true);

    const [pieces, setPieces] = useState<Piece[]>([
        {
            width: 81,
            height: 152,
            count: 2,
        },
        {
            width: 31,
            height: 40,
            count: 2,
        },
    ]);

    const [result, setResult] =
        useState<OptimizeResponse | null>(null);

    return (

        <div className="flex min-h-dvh flex-col bg-slate-100 lg:h-dvh lg:min-h-0">

            <Header />

            {/* 모바일은 위아래로 쌓고 페이지 전체를 스크롤한다. lg부터 좌우 2단 고정 레이아웃. */}
            <main className="flex flex-1 flex-col lg:min-h-0 lg:flex-row lg:overflow-hidden">

                <Sidebar
                    filmWidth={filmWidth}
                    setFilmWidth={setFilmWidth}

                    gap={gap}
                    setGap={setGap}

                    allowRotate={allowRotate}
                    setAllowRotate={setAllowRotate}

                    pieces={pieces}
                    setPieces={setPieces}

                    setResult={setResult}
                />

                <PreviewCanvas result={result} filmWidth={filmWidth} />

            </main>

            <Footer result={result} />

        </div>

    );

}
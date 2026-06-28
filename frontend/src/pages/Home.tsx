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

        <div className="flex h-screen flex-col bg-slate-100">

            <Header />

            <main className="flex flex-1 overflow-hidden">

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

                <PreviewCanvas result={result} />

            </main>

            <Footer result={result} />

        </div>

    );

}
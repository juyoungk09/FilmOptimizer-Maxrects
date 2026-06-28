import type { OptimizeResponse } from "../../types/Response";

interface Props{

    result:OptimizeResponse|null;

}

export default function Footer({result}:Props){
    return(
        <footer className="border-t bg-white px-6 py-4">
            <div className="flex gap-10">
                <div>
                    사용길이
                    <div className="text-xl font-bold">
                        {result?.usedLength ?? 0} mm
                    </div>
                </div>
                <div>
                    폐기율
                    <div className="text-xl font-bold">
                        {result
                            ? (result.wasteRate*100).toFixed(2)
                            : 0
                        }%
                    </div>
                </div>
            </div>
        </footer>
    );
}
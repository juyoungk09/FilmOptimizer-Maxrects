import type { OptimizeResponse } from "../../types/Response";

interface Props{

    result:OptimizeResponse|null;

}

export default function Footer({result}:Props){
    return(
        <footer className="border-t bg-white px-4 py-3 sm:px-6 sm:py-4">
            <div className="grid grid-cols-2 gap-4 sm:flex sm:gap-10">
                <div>
                    사용길이
                    <div className="text-lg font-bold sm:text-xl">
                        {result?.usedLength ?? 0} mm
                    </div>
                </div>
                <div>
                    폐기율
                    <div className="text-lg font-bold sm:text-xl">
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
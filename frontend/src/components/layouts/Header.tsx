export default function Header() {
    return (
        <header className="border-b bg-white px-4 py-3 shadow-sm sm:px-6 sm:py-4 lg:px-8">
            <h1 className="text-xl font-bold sm:text-2xl">
                Film Optimizer
            </h1>

            <p className="text-xs text-slate-500 sm:text-sm">
                MaxRects 기반 필름 최적화 시스템
            </p>
        </header>
    );
}
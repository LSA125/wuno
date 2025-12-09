export default function RequiredLengthGauge({ value, min }: { value: number; min: number }) {
    const pct = Math.max(0, Math.min(1, value / min));
    const met = value >= min;

    return (
        <div className="flex flex-col items-center">
            <div className="relative w-20 h-20">
                <svg viewBox="0 0 36 36" className="w-full h-full">
                    <path
                        d="M18 2.0845
               a 15.9155 15.9155 0 0 1 0 31.831
               a 15.9155 15.9155 0 0 1 0 -31.831"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="1"
                        className="opacity-20"
                    />
                    <path
                        d="M18 2.0845
               a 15.9155 15.9155 0 0 1 0 31.831
               a 15.9155 15.9155 0 0 1 0 -31.831"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeDasharray={`${(pct * 100).toFixed(2)}, 100`}
                        className={`${met ? "text-green-600" : "text-blue-600"} transition-all`}
                    />
                    <text x="18" y="20.35" className="text-[8px]" textAnchor="middle">
                        {value}/{min}
                    </text>
                </svg>
            </div>
            <div className={`text-xs mt-1 ${met ? "text-green-700" : "text-blue-700"}`}>
                {met ? "Requirement met" : "Keep typing"}
            </div>
        </div>
    );
}

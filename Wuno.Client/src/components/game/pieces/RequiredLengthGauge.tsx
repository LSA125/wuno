type RequiredLengthGaugeProps = {
    value: number;
    min: number;
    potentialMinLen?: number;  // What the min len would be after submission
};

export default function RequiredLengthGauge({ 
    value, 
    min,
    potentialMinLen = min 
}: RequiredLengthGaugeProps) {
    const pct = Math.max(0, Math.min(1, value / min));
    const met = value >= min;
    
    // Calculate reduction effect
    const reduction = min - potentialMinLen;
    const hasReduction = reduction > 0;
    const hasOverflow = potentialMinLen < 0;

    return (
        <div className="length-gauge">
            <div className="gauge-visual">
                <svg viewBox="0 0 36 36" className="gauge-svg">
                    {/* Background ring */}
                    <path
                        d="M18 2.0845
               a 15.9155 15.9155 0 0 1 0 31.831
               a 15.9155 15.9155 0 0 1 0 -31.831"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="1"
                        className="gauge-bg"
                    />
                    {/* Progress ring */}
                    <path
                        d="M18 2.0845
               a 15.9155 15.9155 0 0 1 0 31.831
               a 15.9155 15.9155 0 0 1 0 -31.831"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2.5"
                        strokeDasharray={`${(pct * 100).toFixed(2)}, 100`}
                        className={`gauge-progress ${met ? "gauge-met" : "gauge-pending"}`}
                    />
                    {/* Center text */}
                    <text x="18" y="20.35" className="gauge-text" textAnchor="middle">
                        {value}/{min}
                    </text>
                </svg>
            </div>
            <div className={`gauge-label ${met ? "gauge-label-met" : "gauge-label-pending"}`}>
                {met ? "Ready!" : "Keep typing"}
            </div>
            {hasReduction && (
                <div className="gauge-bonus">
                    -{reduction} min len
                    {hasOverflow && <span className="gauge-overflow"> (capped)</span>}
                </div>
            )}
        </div>
    );
}


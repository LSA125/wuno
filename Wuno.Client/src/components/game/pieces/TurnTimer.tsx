import { useCallback, useEffect, useMemo, useState } from "react";
import { DANGER_THRESHOLD_MS } from "@/constants";

type TurnTimerProps = {
    startedAt: string;
    dueAt: string;
    bonusSeconds?: number;    // Potential bonus from current word
    potentialScore?: number;  // Current potential score
};

export default function TurnTimer({
    startedAt,
    dueAt,
    bonusSeconds = 0,
    potentialScore = 0,
}: TurnTimerProps) {
    const startedAtDate = useMemo(() => new Date(startedAt), [startedAt]);
    const dueAtDate = useMemo(() => new Date(dueAt), [dueAt]);

    const computeRemaining = useCallback(() => Math.max(0, dueAtDate.getTime() - Date.now()), [dueAtDate]);

    const [ms, setMs] = useState<number>(() => computeRemaining());
    const totalMs = Math.max(1, dueAtDate.getTime() - startedAtDate.getTime());

    useEffect(() => {
        setMs(computeRemaining());
    }, [computeRemaining]);
    
    useEffect(() => {
        const id = setInterval(() => setMs(computeRemaining()), 100);
        return () => clearInterval(id);
    }, [computeRemaining]);

    const remainingSec = ms / 1000;
    const s = remainingSec.toFixed(1);
    // Remaining percentage (fuse that's left - shrinks from right to left)
    const remainingPct = Math.min(100, Math.max(0, (ms / totalMs) * 100));
    const danger = ms < DANGER_THRESHOLD_MS;
    
    // Calculate bonus bar width (as percentage of total time)
    const bonusMs = bonusSeconds * 1000;
    const bonusPct = Math.min(remainingPct, (bonusMs / totalMs) * 100);
    
    // Calculate overflow (bonus that exceeds remaining capacity)
    const overflowMs = Math.max(0, (bonusMs + ms) - totalMs);
    const overflowPct = (overflowMs / totalMs) * 100;

    return (
        <div className="bomb-timer position-relative w-100" aria-live="polite" role="status">
            {/* Bomb icon */}
            <div className="bomb-icon">
                <span className={`bomb-emoji ${danger ? "bomb-shake" : ""}`}>💣</span>
            </div>
            
            {/* Fuse rope timer bar */}
            <div className="fuse-container">
                <div className="d-flex justify-content-between align-items-center mb-1">
                    <span className="fuse-time">
                        <strong>{s}s</strong> remaining
                    </span>
                    {potentialScore > 0 && (
                        <span className="potential-score badge bg-info">
                            +{potentialScore} pts
                        </span>
                    )}
                </div>
                
                <div className="fuse-track" aria-label="Turn timer">
                    {/* Remaining fuse (unburnt) - anchored to left, shrinks from right */}
                    <div
                        className={`fuse-remaining ${danger ? "fuse-danger" : ""}`}
                        style={{ width: `${remainingPct}%` }}
                    />
                    
                    {/* Bonus time preview (green) - extends right from remaining edge */}
                    {bonusSeconds > 0 && (
                        <div
                            className="fuse-bonus"
                            style={{ 
                                left: `${remainingPct}%`,
                                width: `${bonusPct}%` 
                            }}
                        />
                    )}
                    
                    {/* Overflow indicator (purple, from right edge) */}
                    {overflowPct > 0 && (
                        <div
                            className="fuse-overflow"
                            style={{ width: `${Math.min(overflowPct, 30)}%` }}
                            title={`+${(overflowMs / 1000).toFixed(1)}s overflow`}
                        />
                    )}
                    
                    {/* Spark at burn point (right edge of remaining fuse) */}
                    <div 
                        className="fuse-spark" 
                        style={{ left: `${remainingPct}%` }}
                    />
                </div>
                
                {bonusSeconds > 0 && (
                    <div className="bonus-label">
                        +{bonusSeconds.toFixed(1)}s bonus
                        {overflowPct > 0 && (
                            <span className="overflow-label"> ({(overflowMs / 1000).toFixed(1)}s overflow)</span>
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}

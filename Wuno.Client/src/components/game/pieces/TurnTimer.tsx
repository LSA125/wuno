import { useCallback, useEffect, useMemo, useState } from "react";
import EffectChip from "./EffectChip";
import { EffectEvent } from "./effectTypes";
export default function TurnTimer({
    startedAt,
    dueAt,
    effects,
}: {
    startedAt: string;
    dueAt: string;
    effects: EffectEvent[];
}) {
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
    const s = (ms / 1000).toFixed(1);
    const progress = Math.min(100, Math.max(0, ((totalMs - ms) / totalMs) * 100));
    const danger = ms < 4000;
    return (
        <div
            className={`alert ${danger ? "alert-warning" : "alert-secondary"} mb-0 position-relative w-100`}
            aria-live="polite"
            role="status"
            style={{ overflow: "visible" }}
        >
            <div className="timer-effect-stack" aria-hidden={effects.length === 0}>
                {effects.map((effect) => (
                    <EffectChip key={effect.id} effect={effect} floating />
                ))}
            </div>
            <div className="d-flex align-items-center gap-3">
                <div className="flex-1">
                    Time left: <strong>{s}s</strong>
                    <div className="progress mt-2" style={{ height: 10 }} aria-label="Turn timer">
                        <div
                            className={`progress-bar ${danger ? "bg-danger" : "bg-primary"}`}
                            style={{ width: `${progress}%`, transition: "width 120ms linear" }}
                            aria-valuenow={progress}
                            aria-valuemin={0}
                            aria-valuemax={100}
                        />
                    </div>
                </div>
            </div>
        </div>
    );
}
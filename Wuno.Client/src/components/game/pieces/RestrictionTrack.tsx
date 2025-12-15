import { type ReactNode, useMemo } from "react";
export type RestrictionTrackProps = {
    minLen: number;
    typedWord: string;
    previousWord?: string | null;
    startLetter: string | null;
    reverseMatchLength: number;
    invalid?: boolean;
    requiredWords: number;
    children?: ReactNode;
};

const normalizeWord = (word: string) =>
    word
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toUpperCase()
        .replace(/[^A-Z]/g, "");

export default function RestrictionTrack({
    minLen,
    typedWord,
    previousWord,
    startLetter,
    reverseMatchLength,
    invalid = false,
    requiredWords,
    children,
}: RestrictionTrackProps) {
    const typed = normalizeWord(typedWord || "");
    const prev = normalizeWord(previousWord || "");
    const reversedPrev = useMemo(() => prev.split("").reverse(), [prev]);
    const totalBoxes = Math.max(minLen, typed.length, reversedPrev.length || 0, 4);

    const ready = typed.length >= minLen && (startLetter ? typed.startsWith(normalizeWord(startLetter)) : true);

    return (
        <div className={`restriction-track card border-0 shadow-sm ${invalid ? "shake" : ""}`}>
            <div className="card-body">
                <div className="d-flex justify-content-between align-items-center mb-2">
                    <div>
                        <p className="text-uppercase text-muted small mb-1">Chain requirement</p>
                        <h6 className="mb-0">Match the reverse of the last played word</h6>
                    </div>
                    <span className={`badge ${ready ? "text-bg-success" : "text-bg-warning"}`}>
                        {ready ? "Ready to submit" : "Keep typing"}
                    </span>
                </div>
                <div className="letter-track" role="list" aria-label="Reverse chain tracker">
                    {Array.from({ length: totalBoxes }).map((_, idx) => {
                        const typedChar = typed[idx];
                        const prevChar = reversedPrev[idx];
                        const requirementLetter = startLetter && idx === 0 ? startLetter.toUpperCase() : "";
                        const matchesReverse = idx < reverseMatchLength && Boolean(prevChar);
                        const pending = !typedChar && idx < minLen;
                        const unmetRequirement = requirementLetter && typedChar && typedChar !== requirementLetter;
                        const active = typedChar || prevChar || requirementLetter;
                        let stateClass = "letter-box";
                        if (matchesReverse) stateClass += " match";
                        else if (unmetRequirement) stateClass += " warn";
                        else if (typedChar) stateClass += " typed";
                        else if (pending) stateClass += " pending";
                        else if (active) stateClass += " ghost";

                        return (
                            <div key={idx} className={stateClass} data-letter-index={idx} aria-label={`Letter slot ${idx + 1}`}>
                                <span className="letter-main">{typedChar || requirementLetter || prevChar || "·"}</span>
                                {prevChar && <span className="letter-hint">{prevChar}</span>}
                            </div>
                        );
                    })}
                </div>

                {children && <div className="mt-3">{children}</div>}
                <div className="d-flex flex-wrap gap-3 mt-3 text-muted small">
                    <span>{startLetter ? `Must start with ${startLetter.toUpperCase()}` : "Any start letter allowed"}</span>
                    <span className="dot" aria-hidden="true" />
                    <span>Matching boxes light up as you mirror the last played word.</span>
                    <span className="dot" aria-hidden="true" />
                    <span>Need at least {requiredWords} letters before submitting.</span>
                </div>
            </div>
        </div>
    );
}
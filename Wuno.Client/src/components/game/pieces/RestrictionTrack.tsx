export type RestrictionTrackProps = {
    minLen: number;
    typedWord: string;
    previousWord?: string | null;
    startLetter: string | null;
    freeStart: boolean;
};

export default function RestrictionTrack({ minLen, typedWord, previousWord, startLetter, freeStart }: RestrictionTrackProps) {
    const upperTyped = (typedWord || "").toUpperCase();
    const upperPrev = (previousWord || "").toUpperCase();
    const requirementLetter = !freeStart && startLetter ? startLetter.toUpperCase() : null;
    const boxCount = Math.max(minLen, upperTyped.length, upperPrev.length || 0);

    const ready = upperTyped.length >= minLen && (requirementLetter ? upperTyped.startsWith(requirementLetter) : true);

    return (
        <div className="restriction-track" data-sound-ready={ready ? "true" : undefined}>
            <div className="flex flex-wrap gap-2" role="list" aria-label="Restriction track">
                {Array.from({ length: boxCount }).map((_, idx) => {
                    const typedChar = upperTyped[idx];
                    const prevChar = upperPrev[idx];
                    const matchesPrev = Boolean(typedChar && prevChar && typedChar === prevChar);
                    const needsLetter = !freeStart && idx === 0 && requirementLetter;
                    const unmetRequirement = needsLetter && typedChar !== requirementLetter;
                    const pending = !typedChar && idx < minLen;
                    return (
                        <LetterBox
                            key={idx}
                            index={idx}
                            typed={typedChar}
                            prev={prevChar}
                            matchesPrev={matchesPrev}
                            pending={pending}
                            unmetRequirement={!!unmetRequirement}
                            requirementLetter={needsLetter ? requirementLetter || undefined : undefined}
                        />
                    );
                })}
            </div>
            <div className="mt-2 text-xs text-muted">
                {freeStart ? "Free start" : `Must start with “${(startLetter ?? "").toUpperCase()}”`}
                {ready ? " · Ready to submit" : ""}
            </div>
        </div>
    );
}

type LetterBoxProps = {
    index: number;
    typed?: string;
    prev?: string;
    matchesPrev: boolean;
    pending: boolean;
    unmetRequirement: boolean;
    requirementLetter?: string;
};

function LetterBox({ index, typed, prev, matchesPrev, pending, unmetRequirement, requirementLetter }: LetterBoxProps) {
    const baseClass = "w-10 h-14 rounded border flex flex-col items-center justify-center text-sm font-semibold";
    let stateClass = "bg-white";
    if (matchesPrev) {
        stateClass = "bg-success-subtle border-success text-success";
    } else if (typed) {
        stateClass = "bg-primary-subtle border-primary text-primary";
    } else if (pending) {
        stateClass = "bg-body-tertiary border-dashed";
    }
    if (unmetRequirement) {
        stateClass = "bg-warning-subtle border-warning text-warning";
    }

    return (
        <div
            className={`${baseClass} ${stateClass}`}
            data-letter-index={index}
            data-sound-event={matchesPrev ? "match" : undefined}
            aria-live="polite"
        >
            <span>{typed || requirementLetter || ""}</span>
            {!typed && prev && (
                <span className="opacity-60" style={{ fontSize: "0.65rem" }}>
                    {prev}
                </span>
            )}
        </div>
    );
}
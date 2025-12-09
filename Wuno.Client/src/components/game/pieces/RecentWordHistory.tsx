import type { TurnHistoryState } from "@/api/types";
import EffectChip from "./EffectChip";
import WordPreview from "./WordPreview";

export type RecentWordHistoryProps = {
    history: TurnHistoryState[] | null | undefined;
    fallbackPrevious?: string | null;
};

export default function RecentWordHistory({ history, fallbackPrevious }: RecentWordHistoryProps) {
    const entries = history ?? [];
    const listCount = Math.min(entries.length, 4);
    const recent = entries.slice(-listCount);
    const offset = entries.length - recent.length;

    return (
        <div className="recent-word-inline" aria-label="Recent word history">
            <div className="letter-track recent-word-track" role="list">
                {recent.map((entry, idx) => {
                    const globalIdx = offset + idx;
                    const prevWord = globalIdx > 0 ? entries[globalIdx - 1]?.word : fallbackPrevious || "";
                    return (
                        <div key={entry.turnId} className="letter-box history-box" role="listitem">
                            <div className="d-flex justify-content-between align-items-center w-100 gap-2 text-muted small">
                                <span className="text-uppercase fw-semibold">Seat {entry.seat}</span>
                                <span className="text-xs">Turn #{entry.index + 1}</span>
                            </div>
                            <WordPreview
                                word={entry.word}
                                previousWord={prevWord}
                                minLen={entry.minLen}
                                compact
                                label={`Word from seat ${entry.seat}`}
                                effects={[]}
                            />
                            {entry.effects.length > 0 && (
                                <div className="recent-effect-list" aria-label="Applied effects">
                                    {entry.effects.map((effect, i) => (
                                        <EffectChip key={`${entry.turnId}-${i}`} effect={effect} subtle compact />
                                    ))}
                                </div>
                            )}
                        </div>
                    );
                })}
                {entries.length === 0 && (
                    <div className="letter-box history-box muted" role="listitem">
                        <div className="text-muted small">No words yet this round.</div>
                    </div>
                )}
            </div>
        </div>
    );
}
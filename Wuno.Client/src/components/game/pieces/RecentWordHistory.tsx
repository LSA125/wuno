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
            <div className="d-flex justify-content-between align-items-center mb-2">
                <span className="text-uppercase text-muted small">Recent words</span>
                <span className="badge text-bg-light">{entries.length}</span>
            </div>
            <div className="letter-track recent-word-track" role="list">
                {recent.map((entry, idx) => {
                    const globalIdx = offset + idx;
                    const prevWord = globalIdx > 0 ? entries[globalIdx - 1]?.word : fallbackPrevious || "";
                    return (
                        <div key={entry.turnId} className="letter-box history-box" role="listitem">
                            <div className="d-flex justify-content-between align-items-center w-100 gap-2">
                                <span className="badge text-bg-primary-subtle text-uppercase">Seat {entry.seat}</span>
                                <span className="text-xs text-muted">Turn #{entry.index + 1}</span>
                            </div>
                            <div className="mt-2 w-100">
                                <WordPreview
                                    word={entry.word}
                                    previousWord={prevWord}
                                    minLen={entry.minLen}
                                    compact
                                    label={`Word from seat ${entry.seat}`}
                                    effects={entry.effects}
                                />
                            </div>
                            {entry.effects.length > 0 && (
                                <div className="d-flex flex-wrap gap-1 mt-2">
                                    {entry.effects.map((effect, i) => (
                                        <EffectChip key={`${entry.turnId}-${i}`} effect={effect} subtle />
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
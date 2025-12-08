import type { TurnHistoryState } from "@/api/types";
import WordPreview from "./WordPreview";

export type RecentWordHistoryProps = {
    history: TurnHistoryState[];
    fallbackPrevious?: string | null;
};

export default function RecentWordHistory({ history, fallbackPrevious }: RecentWordHistoryProps) {
    const listCount = Math.min(5, Math.max(3, history.length || 0));
    const recent = history.slice(-listCount);
    const offset = history.length - recent.length;

    return (
        <div className="recent-words card border-0 shadow-sm">
            <div className="card-body">
                <div className="d-flex justify-content-between align-items-center mb-3">
                    <div>
                        <p className="text-uppercase text-muted small mb-1">Recent words</p>
                        <h6 className="mb-0">Words this round</h6>
                    </div>
                    <span className="badge text-bg-light">{history.length}</span>
                </div>
                <ol className="recent-word-list list-unstyled m-0 d-flex flex-column gap-2">
                    {recent.map((entry, idx) => {
                        const globalIdx = offset + idx;
                        const prevWord =
                            globalIdx > 0 ? history[globalIdx - 1]?.word : fallbackPrevious || "";
                        return (
                            <li key={entry.turnId} className="recent-word-row">
                                <div className="d-flex justify-content-between align-items-center gap-3">
                                    <div className="flex-1">
                                        <div className="d-flex align-items-center gap-2 mb-1">
                                            <span className="badge text-bg-primary-subtle text-uppercase">Seat {entry.seat}</span>
                                            <span className="text-xs text-muted">Turn #{entry.index + 1}</span>
                                        </div>
                                        <WordPreview
                                            word={entry.word}
                                            previousWord={prevWord}
                                            minLen={entry.minLen}
                                            compact
                                            label={`Word from seat ${entry.seat}`}
                                            effects={entry.effects}
                                        />
                                    </div>
                                </div>
                            </li>
                        );
                    })}
                    {history.length === 0 && <li className="text-muted small">No words yet this round.</li>}
                </ol>
            </div>
        </div>
    );
}
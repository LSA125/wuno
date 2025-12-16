import { useEffect, useRef } from "react";
import type { TurnHistoryState, PlayerState } from "@/api/types";
import WordPreview from "./WordPreview";

export type RecentWordHistoryProps = {
    history: TurnHistoryState[] | null | undefined;
    fallbackPrevious?: string | null;
    players?: PlayerState[];
};

export default function RecentWordHistory({ history, fallbackPrevious, players = [] }: RecentWordHistoryProps) {
    const scrollRef = useRef<HTMLDivElement>(null);
    const entries = history ?? [];

    // Reverse to show most recent at top
    const reversed = [...entries].reverse();

    // Auto-scroll to top when new word is added
    useEffect(() => {
        if (scrollRef.current) {
            scrollRef.current.scrollTop = 0;
        }
    }, [entries.length]);

    // Helper to get player name by seat
    const getPlayerName = (seat: number) => {
        const player = players.find(p => p.seat === seat);
        return player?.name || "Player";
    };

    if (reversed.length === 0) {
        return (
            <div className="text-muted small text-center py-3">
                No words played yet
            </div>
        );
    }

    return (
        <div className="word-history-scroll" ref={scrollRef} aria-label="Recent word history">
            <div className="word-history-list">
                {reversed.map((entry, idx) => {
                    // Find the previous word for this entry
                    const originalIdx = entries.length - 1 - idx;
                    const prevWord = originalIdx > 0 ? entries[originalIdx - 1]?.word : fallbackPrevious || "";
                    const isLatest = idx === 0;

                    return (
                        <div
                            key={entry.turnId}
                            className={`word-history-item ${isLatest ? "latest" : ""}`}
                        >
                            <div className="d-flex justify-content-between align-items-center mb-2">
                                <span className="fw-semibold small">{getPlayerName(entry.seat)}</span>
                                <div className="d-flex align-items-center gap-2">
                                    {entry.score > 0 && (
                                        <span className="badge bg-success">+{entry.score} pts</span>
                                    )}
                                    <span className="text-muted text-xs">Turn {entry.index + 1}</span>
                                </div>
                            </div>
                            <WordPreview
                                word={entry.word}
                                previousWord={prevWord}
                                minLen={entry.minLen}
                                compact
                                label={`Word by ${getPlayerName(entry.seat)}`}
                                score={entry.score}
                            />
                        </div>
                    );
                })}
            </div>
        </div>
    );
}


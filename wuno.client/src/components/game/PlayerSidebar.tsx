import type { PlayerState } from "@/api/types";
import PlayerTypingRow from "./pieces/PlayerTypingRow";

export type PlayerSidebarProps = {
    players: PlayerState[];
    typedBySeat: Record<number, string>;
    currentSeat?: number | null;
    meSeat: number;
};

export default function PlayerSidebar({ players, typedBySeat, currentSeat, meSeat }: PlayerSidebarProps) {
    const ordered = [...players].sort((a, b) => a.seat - b.seat);
    return (
        <aside className="card shadow h-fit" aria-label="Players">
            <div className="card-header">
                <h6 className="card-title mb-0 text-uppercase text-xs tracking-wide">Players</h6>
            </div>
            <div className="card-body p-0">
                <ul className="divide-y">
                    {ordered.map((player) => (
                        <PlayerTypingRow
                            key={player.playerId}
                            player={player}
                            typed={typedBySeat[player.seat] || player.lastWord || ""}
                            isActiveSeat={currentSeat === player.seat}
                            isViewer={meSeat === player.seat}
                        />
                    ))}
                </ul>
            </div>
        </aside>
    );
}
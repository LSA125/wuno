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
    const activePlayers = ordered.filter((p) => p.isActive);
    const inactivePlayers = ordered.filter((p) => !p.isActive);

    const renderGroup = (groupLabel: string, list: PlayerState[], tone: "success" | "secondary") => (
        <div className="player-group">
            <div className="player-group-heading d-flex justify-content-between align-items-center px-3 py-2">
                <div className="text-uppercase fw-semibold small">{groupLabel}</div>
                <span className={`badge text-bg-${tone}`}>{list.length} player{list.length === 1 ? "" : "s"}</span>
            </div>
            <ul className="list-unstyled m-0 p-0">
                {list.map((player) => (
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
    );
    return (
        <aside className="card shadow h-fit sticky-lg-top top-3" aria-label="Players">
            <div className="card-header">
                <h6 className="card-title mb-0 text-uppercase text-xs tracking-wide">Players</h6>
            </div>
            <div className="card-body p-0">
                {renderGroup("Active", activePlayers, "success")}
                {inactivePlayers.length > 0 && renderGroup("Inactive / eliminated", inactivePlayers, "secondary")}
            </div>
        </aside>
    );
}
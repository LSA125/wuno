import type { PlayerState } from "@/api/types";
import PlayerTypingRow from "./pieces/PlayerTypingRow";

export type PlayerSidebarProps = {
    players: PlayerState[];
    currentSeat?: number | null;
    meSeat: number;
    turnContext?: {
        round: number;
        turn: number;
        seat: number;
        playerName?: string | null;
        requiredLength: number;
        startLetter?: string | null;
        freeStart: boolean;
        wins: number;
    };
};

export default function PlayerSidebar({ players, currentSeat, meSeat, turnContext }: PlayerSidebarProps) {
    const ordered = [...players].sort((a, b) => a.seat - b.seat);
    const activePlayers = ordered.filter((p) => p.isActive);
    const inactivePlayers = ordered.filter((p) => !p.isActive);

    const renderGroup = (groupLabel: string, list: PlayerState[], tone: "success" | "secondary") => (
        <div className="player-group">
            <div className="player-group-heading d-flex justify-content-between align-items-center px-3 py-2">
                <div className="text-uppercase fw-semibold small">{groupLabel}</div>
                <span className={`badge text-bg-${tone}`}>{list.length} player{list.length === 1 ? "" : "s"}</span>
            </div>
            <ul className="list-unstyled m-0 p-0 player-roster-list">
                {list.map((player) => {
                    const isCurrent = currentSeat === player.seat;
                    const isViewer = meSeat === player.seat;
                    const disconnected = !player.isConnected;
                    const inactive = !player.isActive;

                    return (
                        <li
                            key={player.playerId}
                            className={`player-pill ${isCurrent ? "is-current" : ""} ${inactive ? "is-out" : ""} ${isViewer ? "is-self" : ""}`}
                        >
                            <div className="d-flex align-items-center gap-3">
                                <img
                                    src={player.iconUrl || "/avatar.svg"}
                                    className="rounded-circle border flex-shrink-0"
                                    width={44}
                                    height={44}
                                    alt={player.name || "Player avatar"}
                                />

                                <div className="flex-1">
                                    <div className="d-flex flex-wrap align-items-center gap-2">
                                        <div className="fw-semibold leading-tight">{player.name || `Seat ${player.seat}`}</div>
                                        {isViewer && <span className="badge text-bg-info text-uppercase">You</span>}
                                        {isCurrent && <span className="badge text-bg-primary text-uppercase">Current</span>}
                                    </div>
                                    <div className="text-xs text-muted">Wins: {player.roundWins}</div>
                                </div>

                                <div className="d-flex flex-column align-items-end gap-1 text-end">
                                    <span className={`badge ${player.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                                        {player.isActive ? "In" : "Out"}
                                    </span>
                                    {disconnected && <small className="text-danger">Offline</small>}
                                </div>
                            </div>
                        </li>
                    );
                })}
            </ul>
        </div>
    );
    return (
        <aside className="player-roster-card h-fit sticky-lg-top top-3" aria-label="Players">
            <div className="player-roster-heading d-flex justify-content-between align-items-center px-3 py-3">
                <h6 className="card-title mb-0 text-uppercase text-xs tracking-wide">Players</h6>
                <span className="badge text-bg-light">{ordered.length} total</span>
            </div>
            <div className="player-roster-body">
                {renderGroup("Active", activePlayers, "success")}
                {inactivePlayers.length > 0 && renderGroup("Inactive / eliminated", inactivePlayers, "secondary")}
            </div>
        </aside>
    );
}
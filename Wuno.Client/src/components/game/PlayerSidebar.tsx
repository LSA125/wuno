import { useEffect, useState, useCallback } from "react";
import type { PlayerState } from "@/api/types";
import { DANGER_THRESHOLD_MS } from "@/constants";

export type PlayerSidebarProps = {
    players: PlayerState[];
    currentSeat?: number | null;
    meSeat: number;
    turnDueAt?: string | null;
    turnContext?: {
        round: number;
        turn: number;
        playerName?: string | null;
        requiredLength: number;
        startLetter?: string | null;
        wins: number;
    };
};

export default function PlayerSidebar({ players, currentSeat, meSeat, turnDueAt, turnContext }: PlayerSidebarProps) {
    const ordered = [...players].sort((a, b) => a.seat - b.seat);
    const activePlayers = ordered.filter((p) => p.isActive);
    const inactivePlayers = ordered.filter((p) => !p.isActive);

    // Timer state for current player
    const [remainingMs, setRemainingMs] = useState<number>(0);

    const computeRemaining = useCallback(() => {
        if (!turnDueAt) return 0;
        return Math.max(0, new Date(turnDueAt).getTime() - Date.now());
    }, [turnDueAt]);

    useEffect(() => {
        setRemainingMs(computeRemaining());
        const id = setInterval(() => setRemainingMs(computeRemaining()), 100);
        return () => clearInterval(id);
    }, [computeRemaining]);

    const formatTime = (ms: number) => {
        const seconds = Math.ceil(ms / 1000);
        return `${seconds}s`;
    };

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
                    // Show remaining time for all active players
                    const playerRemainingMs = player.remainingTime * 1000;
                    const showTimer = player.isActive && playerRemainingMs > 0;
                    const timerDanger = playerRemainingMs < DANGER_THRESHOLD_MS;

                    return (
                        <li
                            key={player.playerId}
                            className={`player-pill ${isCurrent ? "is-current" : ""} ${inactive ? "is-out" : ""} ${isViewer ? "is-self" : ""}`}
                        >
                            <div className="d-flex align-items-center gap-3">
                                {/* Remaining time on the left */}
                                {showTimer ? (
                                    <div className={`player-timer ${timerDanger ? "danger" : ""}`}>
                                        {Math.ceil(playerRemainingMs / 1000)}s
                                    </div>
                                ) : (
                                    <div className="player-timer" style={{ visibility: "hidden" }}>--</div>
                                )}

                                <img
                                    src={player.iconUrl || "https://api.dicebear.com/7.x/bottts/svg?seed=" + player.playerId}
                                    className="rounded-circle border flex-shrink-0"
                                    width={44}
                                    height={44}
                                    alt={player.name || "Player avatar"}
                                    onError={(e) => {
                                        e.currentTarget.src = "https://api.dicebear.com/7.x/bottts/svg?seed=default";
                                    }}
                                />

                                <div className="flex-1">
                                    <div className="d-flex flex-wrap align-items-center gap-2">
                                        <div className="fw-semibold leading-tight">{player.name || "Player"}</div>
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

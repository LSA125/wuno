import type { PlayerState, InGameStatsResponse } from "@/api/types";
import DefaultAvatar from "@/assets/DefaultAvatar.svg"
import PlayerStatsCard from "./pieces/PlayerStatsCard";

export default function WaitingRoom({
    players,
    mePlayerId,
    onReadyChange,
    playerStats,
}: {
    players: PlayerState[];
    mePlayerId: string | null;
    onReadyChange: (ready: boolean) => void;
    playerStats?: Record<string, InGameStatsResponse>;
}) {
    const me = players.find((p) => p.playerId === mePlayerId);

    return (
        <section className="card shadow-lg">
            <div className="card-header d-flex justify-content-between align-items-center">
                <h5 className="card-title mb-0">Waiting Room</h5>
                <span className="badge text-bg-secondary">{players.length} players</span>
            </div>

            <div className="card-body">
                {players.length < 2 && (
                    <div className="alert alert-info d-flex align-items-center gap-2 mb-3" role="alert">
                        <svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" fill="currentColor" viewBox="0 0 16 16">
                            <path d="M8 16A8 8 0 1 0 8 0a8 8 0 0 0 0 16zm.93-9.412-1 4.705c-.07.34.029.533.304.533.194 0 .487-.07.686-.246l-.088.416c-.287.346-.92.598-1.465.598-.703 0-1.002-.422-.808-1.319l.738-3.468c.064-.293.006-.399-.287-.47l-.451-.081.082-.381 2.29-.287zM8 5.5a1 1 0 1 1 0-2 1 1 0 0 1 0 2z"/>
                        </svg>
                        <span>Waiting for more players. <strong>At least 2 players</strong> are needed to start the game.</span>
                    </div>
                )}
                <ul className="list-group list-group-flush">
                    {players.map((p) => {
                        const stats = playerStats?.[p.playerId];
                        return (
                            <li
                                key={p.playerId}
                                className="list-group-item d-flex align-items-start justify-content-between py-3"
                            >
                                <div className="d-flex align-items-start gap-3">
                                    <img
                                        src={p.iconUrl || DefaultAvatar}
                                        alt={p.name ? `${p.name} avatar` : "Player avatar"}
                                        className="rounded-circle border flex-shrink-0"
                                        style={{
                                            width: 48,
                                            height: 48,
                                            objectFit: "cover",
                                        }}
                                        loading="lazy"
                                        referrerPolicy="no-referrer"
                                    />
                                    <div className="d-flex flex-column">
                                        <div className="fw-semibold">
                                            {p.name || `Seat ${p.seat}`}
                                        </div>
                                        <div className="text-muted small mb-1">
                                            {p.isConnected ? "online" : "offline"}
                                        </div>
                                        {stats && (
                                            <PlayerStatsCard stats={stats} compact />
                                        )}
                                    </div>
                                </div>

                                <div className="d-flex align-items-center gap-2">
                                    <span
                                        className={`badge ${p.isActive ? "text-bg-success" : "text-bg-warning"
                                            }`}
                                    >
                                        {p.isActive ? "Ready" : "Not ready"}
                                    </span>
                                </div>
                            </li>
                        );
                    })}
                </ul>

                {me && (
                    <div className="mt-4 d-flex gap-2">
                        <button
                            type="button"
                            className={`btn ${me.isActive ? "btn-outline-secondary" : "btn-success"
                                } shadow-sm`}
                            onClick={() => onReadyChange(!me.isActive)}
                        >
                            {me.isActive ? "Unready" : "I'm Ready"}
                        </button>
                    </div>
                )}
            </div>
        </section>
    );
}

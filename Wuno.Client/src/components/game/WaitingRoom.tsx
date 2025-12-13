import type { PlayerState } from "@/api/types";
import DefaultAvatar from "@/assets/DefaultAvatar.svg"
export default function WaitingRoom({
    players,
    mePlayerId,
    onReadyChange,
}: {
    players: PlayerState[];
    mePlayerId: string | null;
    onReadyChange: (ready: boolean) => void;
}) {
    const me = players.find((p) => p.playerId === mePlayerId);

    return (
        <section className="card shadow-lg">
            <div className="card-header d-flex justify-content-between align-items-center">
                <h5 className="card-title mb-0">Waiting Room</h5>
                <span className="badge text-bg-secondary">{players.length} players</span>
            </div>

            <div className="card-body">
                <ul className="list-group list-group-flush">
                    {players.map((p) => (
                        <li
                            key={p.playerId}
                            className="list-group-item d-flex align-items-center justify-content-between"
                        >
                            <div className="d-flex align-items-center gap-3">
                                <img
                                    src={p.iconUrl || DefaultAvatar}
                                    alt={p.name ? `${p.name} avatar` : "Player avatar"}
                                    className="rounded-circle border flex-shrink-0"
                                    style={{
                                        width: 32,
                                        height: 32,
                                        objectFit: "cover",
                                    }}
                                    loading="lazy"
                                    referrerPolicy="no-referrer"
                                />
                                <div className="d-flex flex-column">
                                    <div className="fw-semibold small">
                                        {p.name || `Seat ${p.seat}`}
                                    </div>
                                    <div className="text-muted small">
                                        {p.isConnected ? "online" : "offline"} &middot; wins: {p.roundWins}
                                    </div>
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
                    ))}
                </ul>

                {me && (
                    <div className="mt-4 d-flex gap-2">
                        <button
                            type="button"
                            className={`btn ${me.isActive ? "btn-outline-secondary" : "btn-success"
                                } shadow-sm`}
                            onClick={() => onReadyChange(!me.isActive)}
                        >
                            {me.isActive ? "Unready" : "I’m Ready"}
                        </button>
                    </div>
                )}
            </div>
        </section>
    );
}

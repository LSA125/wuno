import type { PlayerState } from "@/api/types";
import DefaultAvatar from "@/assets/avatar.svg";
type PlayerTypingRowProps = {
    player: PlayerState;
    typed: string;
    isActiveSeat: boolean;
    isViewer: boolean;
};

export default function PlayerTypingRow({ player, typed, isActiveSeat, isViewer }: PlayerTypingRowProps) {
    const typedPreview = typed?.trim() || player.lastWord || "";
    const disconnected = !player.isConnected;
    const inactive = !player.isActive;
    return (
        <li
            className={`player-row px-3 py-3 d-flex gap-3 align-items-start ${isActiveSeat ? "current-turn" : ""} ${inactive ? "faded" : ""}`}
            style={isViewer ? { boxShadow: "0 0 0 2px rgba(13,110,253,.3)" } : undefined}
        >
            <img
                src={player.iconUrl || DefaultAvatar}
                className="rounded-circle border flex-shrink-0"
                width={44}
                height={44}
                alt={player.name || "Player avatar"}
            />

            <div className="flex-1">
                <div className="d-flex flex-wrap align-items-center gap-2">
                    <div className="fw-semibold leading-tight">{player.name || `Seat ${player.seat}`}</div>
                    {isViewer && <span className="badge text-bg-info text-uppercase">You</span>}
                    {isActiveSeat && <span className="badge text-bg-primary text-uppercase">Current</span>}
                    {disconnected && <DisconnectPill />}
                </div>
                <div className="text-xs opacity-70">Seat {player.seat} · Wins: {player.roundWins}</div>
                <div className="mt-2 font-mono text-sm tracking-wide" aria-live={isActiveSeat ? "polite" : "off"}>
                    {typedPreview ? <span className="uppercase">{typedPreview}</span> : <span className="opacity-60">Waiting…</span>}
                </div>
            </div>

            <span className={`badge ${player.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                {isActiveSeat ? "Your turn" : player.isActive ? "In" : "Out"}
            </span>
        </li>
    );
}

function DisconnectPill() {
    return (
        <span className="badge text-bg-danger d-inline-flex align-items-center gap-1" title="Disconnected">
            <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M5 12h14" />
                <path d="M12 5v14" />
            </svg>
            Offline
        </span>
    );
}
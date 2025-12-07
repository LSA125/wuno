import type { PlayerState } from "@/api/types";

type PlayerTypingRowProps = {
    player: PlayerState;
    typed: string;
    isActiveSeat: boolean;
    isViewer: boolean;
};

export default function PlayerTypingRow({ player, typed, isActiveSeat, isViewer }: PlayerTypingRowProps) {
    const typedPreview = typed?.trim() || player.lastWord || "";
    const disconnected = !player.isConnected;
    const inactiveClass = player.isActive ? "" : "opacity-60";
    const activeSeatClass = isActiveSeat ? "border-primary bg-body-tertiary" : "border-transparent";
    return (
        <li
            className={`py-3 px-3 flex gap-3 items-center rounded border ${inactiveClass} ${activeSeatClass}`}
            style={isViewer ? { boxShadow: "0 0 0 2px rgba(13,110,253,.3)" } : undefined}
        >
            <img src={player.iconUrl || "/avatar.svg"} className="w-10 h-10 rounded-full border" />

            <div className="flex-1">
                <div className="flex flex-wrap items-center gap-2">
                    <div className="font-semibold leading-tight">
                        {player.name || `Seat ${player.seat}`}
                    </div>
                    {isViewer && <span className="badge text-bg-info text-uppercase">You</span>}
                    {disconnected && <DisconnectPill />}
                </div>
                <div className="text-xs opacity-70">Seat {player.seat} · Wins: {player.roundWins}</div>
                <div className="mt-2 font-mono text-sm tracking-wide" aria-live={isActiveSeat ? "polite" : "off"}>
                    {typedPreview ? (
                        <span className="uppercase">{typedPreview}</span>
                    ) : (
                        <span className="opacity-60">Waiting…</span>
                    )}
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
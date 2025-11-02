import type { PlayerState } from "@/api/types";

export default function PlayerTypingRow({
    player,
    isCurrent,
    typed,
}: {
    player: PlayerState;
    isCurrent: boolean;
    typed: string;
}) {
    return (
        <li className="py-3 flex items-center justify-between">
            <div className="flex items-center gap-3">
                <img src={player.iconUrl || "/avatar.svg"} className="w-8 h-8 rounded-full border" />
                <div>
                    <div className="font-semibold">{player.name || `Seat ${player.seat}`}</div>
                    <div className="text-xs opacity-60">Wins: {player.roundWins}</div>
                </div>
            </div>

            <div className={`flex-1 mx-4 ${isCurrent ? "" : "opacity-60"}`}>
                <div
                    className={`px-3 py-2 rounded border bg-white ${isCurrent ? "shadow pulse-border" : ""}`}
                    style={{
                        minHeight: "2.25rem",
                    }}
                >
                    <span className="tracking-wide">{typed || player.lastWord || ""}</span>
                </div>
            </div>

            <span className={`badge ${player.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                {isCurrent ? "Typing…" : player.isActive ? "In" : "Out"}
            </span>
        </li>
    );
}

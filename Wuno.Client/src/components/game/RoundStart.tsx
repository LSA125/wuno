import type { PlayerState, InGameStatsResponse } from "@/api/types";
import PlayerStatsCard from "./pieces/PlayerStatsCard";

export default function RoundStart({
    players,
    targetWins,
    msRemaining,
    playerStats,
}: {
    players: PlayerState[];
    targetWins: number;
    msRemaining: number;
    playerStats?: Record<string, InGameStatsResponse>;
}) {
    const top = [...players].sort((a, b) => b.roundWins - a.roundWins).slice(0, 3);
    const totalWindow = 3000;
    const pct = Math.max(0, Math.min(100, 100 - (msRemaining / totalWindow) * 100));
    const seconds = (msRemaining / 1000).toFixed(1);
    return (
        <section className="grid md:grid-cols-2 gap-4 items-stretch">
            <div className="card shadow">
                <div className="card-header">
                    <h5 className="card-title mb-0">Leaderboard</h5>
                </div>
                <div className="card-body">
                    <div className="grid grid-cols-3 gap-3 items-end text-center">
                        {Array.from({ length: 3 }).map((_, i) => {
                            const p = top[i];
                            const podiumH = [24, 32, 20][i]; // mid tallest
                            const stats = p ? playerStats?.[p.playerId] : undefined;
                            return (
                                <div key={i} className="flex flex-col items-center">
                                    <div className="text-sm mb-2">{p?.name ?? "—"}</div>
                                    <div className="w-full bg-base-200 border rounded-t-xl flex flex-col items-center justify-end"
                                        style={{ height: `${podiumH}vh` }}>
                                        <div className="mb-1 text-2xl font-bold">{p ? p.roundWins : ""}</div>
                                        {stats && (
                                            <div className="mb-2 text-xs">
                                                <span className="badge bg-success me-1" title="Career Wins">
                                                    🏆 {stats.totalWins}
                                                </span>
                                                <span className="badge bg-secondary" title="Win Rate">
                                                    {stats.winRate}%
                                                </span>
                                            </div>
                                        )}
                                    </div>
                                    <div className="text-xs mt-1 opacity-70">{p ? `Seat ${p.seat}` : ""}</div>
                                    {stats && stats.topWords.length > 0 && (
                                        <div className="mt-1 d-flex flex-wrap gap-1 justify-center">
                                            {stats.topWords.slice(0, 2).map((w, wi) => (
                                                <span key={w.word} className={`badge ${wi === 0 ? 'bg-warning text-dark' : 'bg-secondary'}`} style={{ fontSize: '0.65rem' }}>
                                                    {w.word.toUpperCase()}
                                                </span>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            );
                        })}
                    </div>
                    <div className="mt-4 text-center text-sm opacity-70">First to {targetWins} wins</div>
                </div>
            </div>

            <div className="card shadow flex items-center justify-center gradient-card">
                <div className="card-body flex flex-col items-center justify-center text-center gap-3">
                    <div className="text-uppercase text-muted small">Quick start</div>
                    <div className="display-5 fw-bold">{seconds}s</div>
                    <div className="w-100" aria-label="Round starting timer">
                        <div className="progress" style={{ height: 10 }}>
                            <div className="progress-bar progress-bar-striped progress-bar-animated" style={{ width: `${pct}%` }} />
                        </div>
                    </div>
                    <p className="mb-0">Everyone is ready — the next word chain is about to start.</p>
                </div>
            </div>
        </section>
    );
}

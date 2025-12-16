import type { InGameStatsResponse, TopWordEntry } from "@/api/types";

type PlayerStatsCardProps = {
    stats: InGameStatsResponse | null;
    loading?: boolean;
    compact?: boolean;
};

export default function PlayerStatsCard({ stats, loading = false, compact = false }: PlayerStatsCardProps) {
    if (loading) {
        return (
            <div className="d-flex align-items-center gap-2 text-muted small">
                <div className="spinner-border spinner-border-sm" role="status">
                    <span className="visually-hidden">Loading...</span>
                </div>
                <span>Loading stats...</span>
            </div>
        );
    }

    if (!stats) {
        return null;
    }

    if (compact) {
        return (
            <div className="d-flex flex-wrap gap-2 align-items-center">
                <span className="badge bg-success" title="Games Won">
                    🏆 {stats.totalWins}
                </span>
                <span className="badge bg-secondary" title="Win Rate">
                    {stats.winRate}%
                </span>
                {stats.topWords.length > 0 && (
                    <span className="badge bg-info text-dark" title="Best Word">
                        ⭐ {stats.topWords[0].word.toUpperCase()}
                    </span>
                )}
            </div>
        );
    }

    return (
        <div className="player-stats-card">
            <div className="d-flex flex-wrap gap-2 small">
                <div className="d-flex align-items-center gap-1">
                    <span className="text-success fw-bold">{stats.totalWins}</span>
                    <span className="text-muted">wins</span>
                </div>
                <div className="d-flex align-items-center gap-1">
                    <span className="fw-bold">{stats.gamesPlayed}</span>
                    <span className="text-muted">games</span>
                </div>
                <div className="d-flex align-items-center gap-1">
                    <span className="text-warning fw-bold">{stats.winRate}%</span>
                    <span className="text-muted">rate</span>
                </div>
                {stats.highestScore > 0 && (
                    <div className="d-flex align-items-center gap-1">
                        <span className="text-danger fw-bold">{stats.highestScore}</span>
                        <span className="text-muted">best</span>
                    </div>
                )}
            </div>
            {stats.topWords.length > 0 && (
                <div className="mt-1 d-flex flex-wrap gap-1">
                    {stats.topWords.slice(0, 3).map((w: TopWordEntry, i: number) => (
                        <span
                            key={w.word}
                            className={`badge ${i === 0 ? 'bg-warning text-dark' : 'bg-secondary'}`}
                            title={`${w.score} pts`}
                        >
                            {w.word.toUpperCase()}
                        </span>
                    ))}
                </div>
            )}
        </div>
    );
}

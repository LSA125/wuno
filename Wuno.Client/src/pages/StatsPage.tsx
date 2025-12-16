import { useEffect, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useUser } from "@/context/UserContext";
import { Api } from "@/api/client";
import type { UserStatsResponse, TopWordEntry } from "@/api/types";
import DefaultAvatar from "@/assets/DefaultAvatar.svg";

export default function StatsPage() {
    const { user } = useUser();
    const nav = useNavigate();
    const [stats, setStats] = useState<UserStatsResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!user?.userId) {
            nav("/", { replace: true });
            return;
        }

        (async () => {
            try {
                const result = await Api.getUserStats(user.userId);
                setStats(result);
            } catch (e) {
                setError(e instanceof Error ? e.message : "Failed to load stats");
            } finally {
                setLoading(false);
            }
        })();
    }, [user, nav]);

    if (loading) {
        return (
            <div className="container mx-auto px-4 my-10">
                <div className="card max-w-4xl mx-auto shadow-lg">
                    <div className="card-body text-center">
                        <div className="spinner-border text-primary" role="status">
                            <span className="visually-hidden">Loading...</span>
                        </div>
                        <p className="mt-3">Loading your statistics...</p>
                    </div>
                </div>
            </div>
        );
    }

    if (error || !stats) {
        return (
            <div className="container mx-auto px-4 my-10">
                <div className="card max-w-4xl mx-auto shadow-lg">
                    <div className="card-body text-center">
                        <h3 className="text-danger">Error</h3>
                        <p>{error || "Could not load statistics"}</p>
                        <Link to="/lobby" className="btn btn-primary">
                            Back to Lobby
                        </Link>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 my-10">
            <div className="max-w-4xl mx-auto">
                {/* Header */}
                <div className="d-flex justify-content-between align-items-center mb-4">
                    <h1 className="display-6 fw-bold mb-0">Your Statistics</h1>
                    <Link to="/lobby" className="btn btn-outline-secondary">
                        ← Back to Lobby
                    </Link>
                </div>

                {/* Profile Summary */}
                <div className="card shadow-lg mb-4 gradient-card">
                    <div className="card-body d-flex align-items-center gap-4">
                        <img
                            src={user?.iconUrl || DefaultAvatar}
                            alt="Avatar"
                            className="rounded-circle border"
                            style={{ width: 80, height: 80, objectFit: "cover" }}
                        />
                        <div>
                            <h2 className="h4 mb-1">{user?.name || "Player"}</h2>
                            <p className="text-muted mb-0">
                                {stats.gamesPlayed} games played • Member since joining
                            </p>
                        </div>
                    </div>
                </div>

                {/* Stats Grid */}
                <div className="row g-4 mb-4">
                    {/* Win/Loss Stats */}
                    <div className="col-md-6 col-lg-3">
                        <div className="card h-100 shadow-sm">
                            <div className="card-body text-center">
                                <div className="display-4 fw-bold text-success">{stats.gamesWon}</div>
                                <div className="text-muted small text-uppercase">Games Won</div>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-6 col-lg-3">
                        <div className="card h-100 shadow-sm">
                            <div className="card-body text-center">
                                <div className="display-4 fw-bold text-primary">{stats.gamesPlayed}</div>
                                <div className="text-muted small text-uppercase">Games Played</div>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-6 col-lg-3">
                        <div className="card h-100 shadow-sm">
                            <div className="card-body text-center">
                                <div className="display-4 fw-bold text-warning">{stats.winRate}%</div>
                                <div className="text-muted small text-uppercase">Win Rate</div>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-6 col-lg-3">
                        <div className="card h-100 shadow-sm">
                            <div className="card-body text-center">
                                <div className="display-4 fw-bold text-info">{stats.roundsWon}</div>
                                <div className="text-muted small text-uppercase">Rounds Won</div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Word Stats */}
                <div className="row g-4 mb-4">
                    <div className="col-md-6 col-lg-3">
                        <div className="card h-100 shadow-sm">
                            <div className="card-body text-center">
                                <div className="h2 fw-bold text-secondary">{stats.totalWordsPlayed}</div>
                                <div className="text-muted small text-uppercase">Words Played</div>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-6 col-lg-3">
                        <div className="card h-100 shadow-sm">
                            <div className="card-body text-center">
                                <div className="h2 fw-bold text-secondary">{stats.averageWordLength}</div>
                                <div className="text-muted small text-uppercase">Avg Word Length</div>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-6 col-lg-3">
                        <div className="card h-100 shadow-sm">
                            <div className="card-body text-center">
                                <div className="h2 fw-bold text-danger">{stats.highestSingleRoundScore}</div>
                                <div className="text-muted small text-uppercase">Highest Score</div>
                            </div>
                        </div>
                    </div>
                    <div className="col-md-6 col-lg-3">
                        <div className="card h-100 shadow-sm">
                            <div className="card-body text-center">
                                <div className="h4 fw-bold text-secondary text-truncate" title={stats.longestWord ?? "—"}>
                                    {stats.longestWord?.toUpperCase() || "—"}
                                </div>
                                <div className="text-muted small text-uppercase">Longest Word</div>
                            </div>
                        </div>
                    </div>
                </div>

                {/* Top 3 Words */}
                <div className="card shadow-lg">
                    <div className="card-header">
                        <h5 className="card-title mb-0">🏆 Top 3 Words</h5>
                    </div>
                    <div className="card-body">
                        {stats.topWords.length === 0 ? (
                            <p className="text-muted text-center mb-0">
                                Play some games to see your best words!
                            </p>
                        ) : (
                            <div className="row g-3">
                                {stats.topWords.map((entry: TopWordEntry, i: number) => (
                                    <div key={entry.word} className="col-md-4">
                                        <div className={`card h-100 ${i === 0 ? 'border-warning bg-warning bg-opacity-10' : ''}`}>
                                            <div className="card-body text-center">
                                                <div className="h6 text-muted mb-2">
                                                    {i === 0 ? "🥇" : i === 1 ? "🥈" : "🥉"} #{i + 1}
                                                </div>
                                                <div className="h3 fw-bold text-uppercase mb-2">
                                                    {entry.word}
                                                </div>
                                                <div className="badge bg-primary fs-6">
                                                    {entry.score} points
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}

import CreateGameModal from "./CreateGameModal";
import JoinGameCard from "./JoinGameCard";
import ProfileCard from "./ProfileCard";
import StatsButton from "./StatsButton";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Api } from "@/api/client";
import { clearPendingJoin } from "@/utils/pendingJoin";

export default function CreateOrJoin() {
    const [showCreate, setShowCreate] = useState(false);
    const [matchmaking, setMatchmaking] = useState(false);
    const [matchError, setMatchError] = useState<string | null>(null);
    const nav = useNavigate();

    const handleQuickPlay = async () => {
        setMatchError(null);
        setMatchmaking(true);
        try {
            const res = await Api.matchmake();
            if (res.ok) {
                clearPendingJoin();
                nav(`/game/${res.gameCode}`);
            } else {
                setMatchError("Matchmaking failed. Please try again.");
            }
        } catch (e: any) {
            setMatchError(e.message || "Matchmaking failed.");
        } finally {
            setMatchmaking(false);
        }
    };

    return (
        <div className="grid gap-6 md:grid-cols-3 lg:grid-cols-4">
            <div className="md:col-span-2 lg:col-span-3">
                <div className="card shadow-xl">
                    <div className="card-body">
                        <h3 className="card-title text-3xl mb-2">Lobby</h3>
                        <p className="opacity-70 mb-4">
                            Create a new game or join an existing one. You can manage your temporary
                            or registered account at any time.
                        </p>
                        <div className="flex flex-wrap gap-3">
                            <button 
                                className="btn btn-success" 
                                onClick={handleQuickPlay}
                                disabled={matchmaking}
                            >
                                {matchmaking ? (
                                    <>
                                        <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                                        Finding Game...
                                    </>
                                ) : (
                                    "⚡ Quick Play"
                                )}
                            </button>
                            <button className="btn btn-primary" onClick={() => setShowCreate(true)}>
                                Create Game
                            </button>
                            <StatsButton />
                        </div>
                        {matchError && (
                            <div className="alert alert-danger mt-3 py-2">{matchError}</div>
                        )}
                    </div>
                </div>

                <div className="mt-6">
                    <JoinGameCard />
                </div>
            </div>

            <aside>
                <ProfileCard />
            </aside>

            <CreateGameModal open={showCreate} onClose={() => setShowCreate(false)} />
        </div>
    );
}

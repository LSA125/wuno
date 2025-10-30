import { useState } from "react";
import { Api } from "@/api/client";
import type { NewGameRequest } from "@/api/types";
import { useNavigate } from "react-router-dom";

export default function CreateGameModal({
    open,
    onClose,
}: {
    open: boolean;
    onClose: () => void;
}) {
    const [playerCount, setPlayerCount] = useState(4);
    const [targetWins, setTargetWins] = useState(3);
    const [err, setErr] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);
    const nav = useNavigate();

    const submit = async () => {
        setErr(null);
        if (playerCount < 2 || playerCount > 8) { setErr("Players must be between 2 and 8."); return; }
        if (targetWins < 1 || targetWins > 10) { setErr("Target wins must be 1–10."); return; }
        setBusy(true);
        try {
            const req: NewGameRequest = { playerCount, targetWins };
            const res = await Api.createGame(req);
            // ready for future /game route; for now we keep you in lobby:
            // nav(`/game/${res.gameId}`);
            onClose();
        } catch (e: any) {
            setErr(e.message || "Failed to create game.");
        } finally {
            setBusy(false);
        }
    };

    if (!open) return null;
    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4">
            <div className="card w-full max-w-lg shadow-2xl">
                <div className="card-header">
                    <h5 className="card-title m-0">Create a Game</h5>
                </div>
                <div className="card-body">
                    <div className="row g-3">
                        <div className="col-6">
                            <label className="form-label"># Players</label>
                            <input
                                className="form-control"
                                type="number"
                                value={playerCount}
                                min={2}
                                max={8}
                                onChange={(e) => setPlayerCount(parseInt(e.target.value || "0", 10))}
                            />
                        </div>
                        <div className="col-6">
                            <label className="form-label">Target Wins</label>
                            <input
                                className="form-control"
                                type="number"
                                value={targetWins}
                                min={1}
                                max={10}
                                onChange={(e) => setTargetWins(parseInt(e.target.value || "0", 10))}
                            />
                        </div>
                    </div>
                    {err && <div className="alert alert-danger mt-3 py-2">{err}</div>}
                </div>
                <div className="card-footer flex gap-2 justify-end">
                    <button className="btn btn-secondary" onClick={onClose} disabled={busy}>Close</button>
                    <button className="btn btn-primary" onClick={submit} disabled={busy}>
                        {busy ? "Creating..." : "Create Game"}
                    </button>
                </div>
            </div>
        </div>
    );
}

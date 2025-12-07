import { useState } from "react";
import { createGameHub } from "@/hub/connection";
import { HubConnection } from "@microsoft/signalr";
import { useUser } from "@/context/UserContext";

export default function JoinGameCard() {
    const [code, setCode] = useState("");
    const [err, setErr] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);
    const { user } = useUser();

    const join = async () => {
        setErr(null);
        if (!code.trim()) { setErr("Enter a game code."); return; }
        if (!user?.userId) { setErr("No user. Please create a profile first."); return; }

        setBusy(true);
        try {
            const hub: HubConnection = createGameHub();
            await hub.start();
            await hub.invoke("ConnectToGame", code.trim(), user.userId, null);
            // future: redirect to /game/:id after receiving "ConnectedToGame"
            // hub.on("ConnectedToGame", (res: JoinGameResponse) => nav(`/game/${res.state.gameId}`));
        } catch (e: any) {
            setErr(e.message || "Failed to connect.");
        } finally {
            setBusy(false);
        }
    };

    return (
        <div className="card shadow">
            <div className="card-body">
                <h5 className="card-title">Join a Game</h5>
                <div className="row g-3 items-end">
                    <div className="col-8">
                        <label className="form-label">Game Code</label>
                        <input className="form-control" value={code} onChange={e => setCode(e.target.value.toUpperCase())} />
                    </div>
                    <div className="col-4">
                        <button className="btn btn-success w-full mt-4" onClick={join} disabled={busy}>
                            {busy ? "Joining..." : "Join"}
                        </button>
                    </div>
                </div>
                {err && <div className="alert alert-danger mt-3 py-2">{err}</div>}
            </div>
        </div>
    );
}

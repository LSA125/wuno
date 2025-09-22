import { useState } from "react";
import "./App.css";

const API = import.meta.env.VITE_API ?? "";

export default function App() {
    const [data, setData] = useState(null);
    const [count, setCount] = useState(2);
    const [loading, setLoading] = useState(false);
    const [err, setErr] = useState("");

    async function createGame() {
        try {
            setLoading(true); setErr("");
            const res = await fetch(`${API}/api/hotseat/new`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ playerCount: count, targetWins: 2 })
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const json = await res.json();
            setData(json);
        } catch (e) {
            setErr(String(e));
        } finally {
            setLoading(false);
        }
    }

    return (
        <main className="card">
            <h1>WUNO Client</h1>

            <label>
                Players:&nbsp;
                <input
                    type="number" min={2} max={8}
                    value={count}
                    onChange={e => setCount(Number(e.target.value))}
                />
            </label>

            <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                <button onClick={createGame} disabled={loading}>Create Game</button>
                <button disabled>Join Game (later)</button>
            </div>

            {err && <p style={{ color: "crimson" }}>Error: {err}</p>}

            <h2>Response</h2>
            <pre style={{ background: "#111", color: "#0f0", padding: 12 }}>
                {data ? JSON.stringify(data, null, 2) : "No game yet"}
            </pre>
        </main>
    );
}

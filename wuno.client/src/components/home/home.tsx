import { useEffect, useRef, useState } from "react";
import ConfirmModal from "./modal";
import { ClipLoader } from "react-spinners";

export default function Home() {
    const [data, setData] = useState<any>(null);
    const [count, setCount] = useState(2);
    const [targetWins, setTargetWins] = useState(2);
    const [loading, setLoading] = useState(false);
    const [err, setErr] = useState("");

    // Modal state
    const [showModal, setShowModal] = useState(false);
    const [modalCount, setModalCount] = useState(2);
    const [modalWins, setModalWins] = useState(2);

    function openModal() {
        setModalCount(count);
        setModalWins(targetWins);
        setShowModal(true);
    }
    const overlay: React.CSSProperties = {
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.55)",
        backdropFilter: "blur(1px)",
        display: "grid",
        placeItems: "center",
        zIndex: 1000
    };

    async function createGame(c: number, w: number) {
        try {
            setLoading(true); setErr("");
            const res = await fetch(`/api/hotseat/new`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ playerCount: c, targetWins: w })
            });
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            const json = await res.json();
            setData(json);
            // persist chosen values back to main controls (optional)
            setCount(c);
            setTargetWins(w);
        } catch (e) {
            setErr(String(e));
        } finally {
            setLoading(false);
        }
    }

    return (
        <main className="card" style={{ position: "relative" }}>
            <h1>WUNO Client</h1>

            <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
                <button onClick={openModal} disabled={loading}>Create Game</button>
                <button disabled>Join Game (later)</button>
            </div>

            {err && <p style={{ color: "crimson" }}>Error: {err}</p>}

            <h2>Response</h2>
            <pre style={{ background: "#111", color: "#0f0", padding: 12 }}>
                {data ? JSON.stringify(data, null, 2) : "No game yet"}
            </pre>

            {/* Modal */}
            {showModal && (
                <ConfirmModal
                    count={modalCount}
                    wins={modalWins}
                    onChangeCount={setModalCount}
                    onChangeWins={setModalWins}
                    onCancel={() => setShowModal(false)}
                    onConfirm={async () => {
                        setShowModal(false);
                        await createGame(modalCount, modalWins);
                    }}
                />
            )}
            {/* Spinner */}
            {loading && <div style={overlay}>
                <ClipLoader
                    color="#0f0"
                    loading={loading}
                    size={48}
                    cssOverride={{
                        position: "absolute",
                        top: "50%",
                        left: "50%",
                        transform: "translate(-50%, -50%)"
                    }}
                    aria-label="Loading Spinner"
                    data-testid="loader"
                />
            </div>}
        </main>
    );
}
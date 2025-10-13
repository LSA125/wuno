import { useEffect, useRef, useState } from "react";
export default function ConfirmModal({
    count, wins, onChangeCount, onChangeWins, onCancel, onConfirm
}: {
    count: number;
    wins: number;
    onChangeCount: (n: number) => void;
    onChangeWins: (n: number) => void;
    onCancel: () => void;
    onConfirm: () => void;
}) {
    const firstInputRef = useRef<HTMLInputElement>(null);

    // Focus first field & handle Esc/Enter
    useEffect(() => {
        firstInputRef.current?.focus();
        function onKey(e: KeyboardEvent) {
            if (e.key === "Escape") onCancel();
            if (e.key === "Enter") onConfirm();
        }
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [onCancel, onConfirm]);

    const overlay: React.CSSProperties = {
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.55)",
        backdropFilter: "blur(1px)",
        display: "grid",
        placeItems: "center",
        zIndex: 1000
    };

    const modal: React.CSSProperties = {
        width: "min(92vw, 480px)",
        borderRadius: 16,
        background: "linear-gradient(180deg, #151515, #0f0f0f)",
        color: "#eee",
        boxShadow:
            "0 10px 30px rgba(0,0,0,.6), inset 0 1px 0 rgba(255,255,255,.05)",
        padding: "20px 40px 20px 40px",
        border: "1px solid rgba(255,255,255,0.08)"
    };

    const header: React.CSSProperties = {
        fontSize: 20,
        marginBottom: 12
    };

    const row: React.CSSProperties = {
        display: "grid",
        gridTemplateColumns: "1fr 120px",
        alignItems: "center",
        gap: 12,
        margin: "10px 0"
    };

    const input: React.CSSProperties = {
        width: "100%",
        padding: "8px 10px",
        borderRadius: 10,
        border: "1px solid rgba(255,255,255,0.15)",
        background: "#1c1c1c",
        color: "white",
        outline: "none"
    };

    const actions: React.CSSProperties = {
        display: "flex",
        justifyContent: "flex-end",
        gap: 8,
        marginTop: 16
    };

    const button = (variant: "ghost" | "primary"): React.CSSProperties => ({
        padding: "10px 14px",
        borderRadius: 12,
        border: variant === "ghost" ? "1px solid rgba(255,255,255,0.15)" : "none",
        background:
            variant === "primary"
                ? "linear-gradient(180deg, #5ad36a, #2fb24a)"
                : "transparent",
        color: variant === "primary" ? "#0b1d0f" : "#ddd",
        fontWeight: 600,
        cursor: "pointer"
    });

    return (
        <div style={overlay} role="dialog" aria-modal="true" onClick={onCancel}>
            <div style={modal} onClick={e => e.stopPropagation()}>
                <div style={header}>Create Game</div>
                <p style={{ opacity: 0.8, marginTop: 0 }}>
                    Confirm your settings and we’ll spin up a new match.
                </p>

                <div style={row}>
                    <label htmlFor="players">Players</label>
                    <input
                        id="players"
                        ref={firstInputRef}
                        style={input}
                        type="number"
                        min={2}
                        max={8}
                        value={count}
                        onChange={(e) => onChangeCount(Number(e.target.value))}
                    />
                </div>

                <div style={row}>
                    <label htmlFor="wins">Target wins</label>
                    <input
                        id="wins"
                        style={input}
                        type="number"
                        min={1}
                        max={10}
                        value={wins}
                        onChange={(e) => onChangeWins(Number(e.target.value))}
                    />
                </div>

                <div style={actions}>
                    <button style={button("ghost")} onClick={onCancel}>Cancel</button>
                    <button style={button("primary")} onClick={onConfirm}>Create</button>
                </div>
            </div>
        </div>
    );
}

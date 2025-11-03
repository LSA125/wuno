import { useEffect, useMemo, useState } from "react";
import type { GameState } from "@/api/types";
import EffectChip from "./pieces/EffectChip";
import RequiredLengthGauge from "./pieces/RequiredLengthGauge";
import PlayerTypingRow from "./pieces/PlayerTypingRow";

export default function LiveGame({ ...props }) {
    const { state, meSeat, typedBySeat, onType, onSubmit, effectsFlash, ended } = props;

    const turn = state.currentTurn;
    const players = state.players;

    if (!turn) {
        // Turn not ready yet – show a lightweight placeholder
        return (
            <section className="grid lg:grid-cols-3 gap-4">
                <div className="card shadow lg:col-span-2">
                    <div className="card-header">
                        <h5 className="card-title mb-0">Round {state.currentRound.index + 1}</h5>
                    </div>
                    <div className="card-body">
                        <div className="text-muted">Preparing next turn…</div>
                        <ul className="divide-y mt-3">
                            {players.map(p => (
                                <PlayerTypingRow key={p.playerId} player={p} isCurrent={false} typed={typedBySeat[p.seat] || ""} />
                            ))}
                        </ul>
                    </div>
                </div>
                <div className="card shadow">
                    <div className="card-body text-muted">Please wait…</div>
                </div>
            </section>
        );
    }

    const myTurn = meSeat === turn.seat;
    const startLetter = turn.freeStart ? null : (players.find(p => p.seat === turn.seat)?.lastWord?.slice(-1) ?? null);
    const [input, setInput] = useState("");
    const myTyped = typedBySeat[turn.seat] ?? (myTurn ? input : "");
    const minLen = turn.minLen;

    const canSubmit = useMemo(() => {
        const w = input.trim();
        if (!myTurn) return false;
        if (turn.freeStart) return w.length >= minLen;
        if (!w) return false;
        return w.length >= minLen && (startLetter ? w.toLowerCase().startsWith(startLetter.toLowerCase()) : true);
    }, [input, myTurn, minLen, turn.freeStart, startLetter]);

    return (
        <section className="grid lg:grid-cols-3 gap-4">
            {/* Players + live typing */}
            <div className="card shadow lg:col-span-2">
                <div className="card-header d-flex justify-between items-center">
                    <h5 className="card-title mb-0">Round {state.currentRound.index + 1}</h5>
                    <span className="badge text-bg-light">Turn #{turn.index + 1} &middot; Seat {turn.seat}</span>
                </div>
                <div className="card-body">
                    <ul className="divide-y">
                        {state.players.map(p => (
                            <PlayerTypingRow
                                key={p.playerId}
                                player={p}
                                isCurrent={p.seat === turn.seat}
                                typed={p.seat === turn.seat ? myTyped : (typedBySeat[p.seat] || "")}
                            />
                        ))}
                    </ul>
                </div>
            </div>

            {/* Constraints + input */}
            <div className="card shadow relative overflow-hidden">
                <div className="absolute right-2 top-2 flex gap-2">
                    {effectsFlash.map((e, i) => <EffectChip key={i} label={e} />)}
                </div>

                <div className="card-header">
                    <h5 className="card-title mb-0">Your Move</h5>
                </div>

                <div className="card-body">
                    <div className="flex items-center gap-4">
                        <RequiredLengthGauge value={(myTyped || "").length} min={minLen} />
                        <div className="flex-1">
                            <div className="mb-2 text-sm">
                                {turn.freeStart ? (
                                    <span className="badge text-bg-success">Free start</span>
                                ) : (
                                    <span className="badge text-bg-primary">
                                        Must start with “{(startLetter ?? "").toUpperCase()}”
                                    </span>
                                )}
                            </div>

                            <input
                                disabled={!myTurn || ended}
                                className="form-control shadow-sm"
                                placeholder={myTurn ? "Type your word…" : "Waiting for your turn…"}
                                value={input}
                                onChange={(e) => {
                                    const v = e.target.value;
                                    setInput(v);
                                    if (myTurn) onType(meSeat, v);
                                }}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter" && canSubmit) {
                                        onSubmit(input.trim());
                                        setInput("");
                                    }
                                }}
                            />

                            <div className="mt-3 d-flex gap-2">
                                <button
                                    className="btn btn-primary"
                                    disabled={!canSubmit || ended}
                                    onClick={() => {
                                        if (!canSubmit) return;
                                        onSubmit(input.trim());
                                        setInput("");
                                    }}
                                >
                                    Submit
                                </button>
                                <div className="text-sm opacity-70 self-center">
                                    {ended ? "Match finished." : myTurn ? "Press Enter to submit." : "It’s not your turn."}
                                </div>
                            </div>
                        </div>
                    </div>

                    {/* Timer */}
                    <TurnTimer dueAt={state.currentTurn.dueAt} />
                </div>
            </div>
        </section>
    );
}

function TurnTimer({ dueAt }: { dueAt: string }) {
    const [ms, setMs] = useState<number>(() => Math.max(0, new Date(dueAt).getTime() - Date.now()));
    useEffect(() => {
        const id = setInterval(() => setMs(Math.max(0, new Date(dueAt).getTime() - Date.now())), 100);
        return () => clearInterval(id);
    }, [dueAt]);
    const s = (ms / 1000).toFixed(1);
    const danger = ms < 4000;
    return (
        <div className={`mt-4 alert ${danger ? "alert-warning" : "alert-secondary"} mb-0`}>
            Time left: <strong>{s}s</strong>
        </div>
    );
}

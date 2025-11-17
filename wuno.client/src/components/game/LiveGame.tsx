import { useEffect, useMemo, useState } from "react";
import type { GameState, PlayerState, TurnState } from "@/api/types";
import EffectChip from "./pieces/EffectChip";
import RequiredLengthGauge from "./pieces/RequiredLengthGauge";
import PlayerSidebar from "./PlayerSidebar";
import RestrictionTrack from "./pieces/RestrictionTrack";

type LiveGameProps = {
    state: GameState;
    meSeat: number;
    typedBySeat: Record<number, string>;
    onType: (seat: number, word: string) => void;
    onSubmit: (word: string) => void;
    onLeave: () => void;
    canLeave?: boolean;
    effectsFlash: string[];
    ended: boolean;
    currentTurn: TurnState | null;
};
export default function LiveGame({
    state,
    meSeat,
    typedBySeat,
    onType,
    onSubmit,
    onLeave,
    canLeave = true,
    effectsFlash,
    ended,
    currentTurn,
}: LiveGameProps) {

    const turn: TurnState | null = currentTurn;
    const players = state.players;

    if (!turn) {
        return (
            <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr),320px]">
                <div className="card shadow">
                    <div className="card-header">
                        <h5 className="card-title mb-0">Round {state.currentRound.index + 1}</h5>
                    </div>
                    <div className="card-body">
                        <div className="text-muted">Preparing next turn…</div>
                    </div>
                </div>
                <PlayerSidebar players={players} typedBySeat={typedBySeat} currentSeat={state.nextSeat} meSeat={meSeat} />
            </section>
        );
    }

    const myTurn = meSeat === turn.seat;
    const startLetter = turn.freeStart
        ? null
        : players.find((p: PlayerState) => p.seat === turn.seat)?.lastWord?.slice(-1) ?? null;    const [input, setInput] = useState("");
    const myTyped = typedBySeat[turn.seat] ?? (myTurn ? input : "");
    const minLen = turn.minLen;

    const canSubmit = useMemo(() => {
        const w = input.trim();
        if (!myTurn) return false;
        if (turn.freeStart) return w.length >= minLen;
        if (!w) return false;
        return w.length >= minLen && (startLetter ? w.toLowerCase().startsWith(startLetter.toLowerCase()) : true);
    }, [input, myTurn, minLen, turn.freeStart, startLetter]);
    const currentPlayer = players.find((p) => p.seat === turn.seat);
    return (
        <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr),320px]">
            <div className="card shadow relative overflow-hidden" data-sound-turn={myTurn ? "active" : undefined}>
                <div className="absolute right-2 top-2 flex gap-2">
                    {effectsFlash.map((e, i) => (
                        <EffectChip key={i} label={e} />
                    ))}
                </div>

                <div className="card-header">
                    <div className="flex flex-wrap justify-between gap-3 items-center">
                        <div>
                            <h5 className="card-title mb-1">Round {state.currentRound.index + 1}</h5>
                            <div className="text-sm text-muted">Turn #{turn.index + 1} · Seat {turn.seat}</div>
                        </div>
                        <button type="button" className="btn btn-outline-danger" onClick={onLeave} disabled={!canLeave}>
                            Leave game
                        </button>
                    </div>
                </div>

                <div className="card-body flex flex-col gap-4">
                    <RestrictionTrack
                        minLen={minLen}
                        typedWord={myTyped || ""}
                        previousWord={currentPlayer?.lastWord || ""}
                        startLetter={startLetter}
                        freeStart={turn.freeStart}
                    />

                    <div className="flex items-center gap-4 flex-wrap">
                        <RequiredLengthGauge value={(myTyped || "").length} min={minLen} />
                        <div className="flex-1 min-w-[220px]">
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

                    <TurnTimer dueAt={state.currentTurn.dueAt} />
                </div>
            </div>
            <PlayerSidebar
                players={players}
                typedBySeat={{
                    ...typedBySeat,
                    [turn.seat]: myTyped,
                }}
                currentSeat={turn.seat}
                meSeat={meSeat}
            />
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

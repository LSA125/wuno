import { useEffect, useRef, useState } from "react";
import type { GameState, TurnState } from "@/api/types";
import EffectChip from "./pieces/EffectChip";
import RequiredLengthGauge from "./pieces/RequiredLengthGauge";
import PlayerSidebar from "./PlayerSidebar";
import RestrictionTrack from "./pieces/RestrictionTrack";
const normalizeWord = (word: string) =>
    word
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .replace(/[^a-z]/g, "");

const reverseString = (str: string) => str.split("").reverse().join("");

const computeReverseMatchLength = (typed: string, reversed: string) => {
    let len = 0;
    while (len < typed.length && len < reversed.length && typed[len] === reversed[len]) len++;
    return len;
};
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
    const [input, setInput] = useState("");
    const [invalidReason, setInvalidReason] = useState<string | null>(null);
    const [shake, setShake] = useState(false);
    const [wordHistory, setWordHistory] = useState<Set<string>>(new Set());
    const lastMatchRef = useRef(0);
    const audioRef = useRef<AudioContext | null>(null);

    useEffect(() => {
        const nextHistory = new Set<string>();
        players.forEach((p) => {
            if (p.lastWord) nextHistory.add(normalizeWord(p.lastWord));
        });
        setWordHistory(nextHistory);
        setInput("");
        setInvalidReason(null);
        setShake(false);
        lastMatchRef.current = 0;
    }, [players, state.currentRound?.roundId, turn?.turnId]);

    const currentPlayer = players.find((p) => p.seat === turn?.seat);
    const previousWord = currentPlayer?.lastWord ?? "";
    const reversedPrevious = reverseString(normalizeWord(previousWord));
    const normalizedInput = normalizeWord(input);
    const reverseMatchLength = computeReverseMatchLength(normalizedInput, reversedPrevious);
    const requiredStart = !turn?.freeStart && previousWord ? previousWord.slice(-1) : null;

    // Gentle tone that increases pitch as more letters match the reverse chain
    useEffect(() => {
        if (!turn || reverseMatchLength <= lastMatchRef.current || !normalizedInput) return;
        const ctx = (audioRef.current ??= new AudioContext());
        const oscillator = ctx.createOscillator();
        const gain = ctx.createGain();
        oscillator.type = "sine";
        oscillator.frequency.value = 260 + reverseMatchLength * 35;
        gain.gain.value = 0.05;
        oscillator.connect(gain).connect(ctx.destination);
        oscillator.start();
        oscillator.stop(ctx.currentTime + 0.15);
        lastMatchRef.current = reverseMatchLength;
    }, [reverseMatchLength, normalizedInput, turn]);

    const myTurn = meSeat === turn?.seat ?? false;
    const minLen = turn?.minLen ?? 0;
    if (!turn) {
        return (
            <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr),320px]">
                <div className="card shadow glow-card">
                    <div className="card-header">
                        <h5 className="card-title mb-0">Preparing next turn…</h5>
                        <p className="text-muted mb-0">Stay limber — the countdown is almost done.</p>
                    </div>
                    <div className="card-body">
                        <div className="progress" aria-label="Loading turn">
                            <div className="progress-bar progress-bar-striped progress-bar-animated" style={{ width: "75%" }} />
                        </div>
                    </div>
                </div>
                <PlayerSidebar players={players} typedBySeat={typedBySeat} currentSeat={state.nextSeat} meSeat={meSeat} />
            </section>
        );
    }
    const validateWord = (word: string): string | null => {
        const trimmed = word.trim();
        if (!myTurn) return "Wait for your spotlight.";
        if (!trimmed) return "Type a word to submit.";
        const normalized = normalizeWord(trimmed);
        if (!/^[a-z]+$/i.test(trimmed)) return "Letters only, no symbols.";
        if (normalized.length < minLen) return `Need at least ${minLen} letters.`;
        if (!turn.freeStart && requiredStart && normalized[0] !== normalizeWord(requiredStart)[0]) {
            return `Must start with '${requiredStart.toUpperCase()}'`;
        }
        if (reversedPrevious && reverseMatchLength < Math.min(reversedPrevious.length, normalized.length)) {
            return "Follow the reverse chain to keep the streak alive.";
        }
        if (wordHistory.has(normalized)) return "That word already appeared this match.";
        return null;
    };

    const canSubmit = !ended && validateWord(input) === null;

    const attemptSubmit = () => {
        const reason = validateWord(input);
        if (reason) {
            setInvalidReason(reason);
            setShake(true);
            setTimeout(() => setShake(false), 350);
            return;
        }
        const trimmed = input.trim();
        if (!trimmed) return;
        onSubmit(trimmed);
        setWordHistory((prev) => new Set(prev).add(normalizeWord(trimmed)));
        setInput("");
        setInvalidReason(null);
        setShake(false);
        lastMatchRef.current = 0;
    };

    const myTyped = typedBySeat[turn.seat] ?? (myTurn ? input : "");
    const totalLettersNeeded = minLen;
    const roundIndex = (state.currentRound?.index ?? 0) + 1;

    if (ended) {
        const winner = [...players].sort((a, b) => b.roundWins - a.roundWins)[0];
        return (
            <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr),320px]">
                <div className="card shadow-lg text-center gradient-card">
                    <div className="card-body py-5">
                        <p className="text-uppercase text-muted tracking-wide mb-2">Match finished</p>
                        <h2 className="display-6 fw-bold mb-3">{winner ? `${winner.name || "Player"} leads the lobby!` : "Game over"}</h2>
                        <p className="lead mb-4">Kick back or jump into another lobby — everyone sees their final standings on the right.</p>
                        <button type="button" className="btn btn-lg btn-primary px-4" onClick={onLeave}>
                            Back to lobby
                        </button>
                    </div>
                </div>
                <PlayerSidebar players={players} typedBySeat={typedBySeat} currentSeat={turn.seat} meSeat={meSeat} />
            </section>
        );
    }
    return (
        <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr),360px]">
            <div className="card shadow relative overflow-hidden gradient-card" data-sound-turn={myTurn ? "active" : undefined}>
                <div className="absolute right-2 top-2 flex gap-2">
                    {effectsFlash.map((e, i) => (
                        <EffectChip key={i} label={e} />
                    ))}
                </div>

                <div className="card-header bg-transparent border-0 pb-0">
                    <div className="d-flex flex-wrap justify-content-between align-items-center gap-3">
                        <div>
                            <p className="text-uppercase text-muted small mb-1">Round {roundIndex}</p>
                            <h4 className="card-title mb-0">Turn #{turn.index + 1} · Seat {turn.seat}</h4>
                        </div>
                        <div className="d-flex gap-2">
                            <span className="badge text-bg-primary">Need {totalLettersNeeded} letters</span>
                            <button type="button" className="btn btn-outline-danger" onClick={onLeave} disabled={!canLeave}>
                                Leave game
                            </button>
                        </div>
                    </div>
                </div>

                <div className="card-body flex flex-column gap-4">
                    <TurnTimer startedAt={turn.startedAt} dueAt={turn.dueAt} />
                    <RestrictionTrack
                        minLen={minLen}
                        typedWord={myTyped || ""}
                        previousWord={previousWord || ""}
                        startLetter={requiredStart}
                        freeStart={turn.freeStart}
                        reverseMatchLength={reverseMatchLength}
                        invalid={shake}
                        requiredWords={totalLettersNeeded}
                        freeStart={turn.freeStart}
                    />

                    <div className="flex flex-wrap gap-4 align-items-center">
                        <RequiredLengthGauge value={(myTyped || "").length} min={minLen} />
                        <div className="flex-1 min-w-[260px]">
                            <label className="form-label fw-semibold" htmlFor="typed-word">
                                {myTurn ? "Your turn" : "Waiting for your turn"}
                            </label>
                            <div className={`input-group input-group-lg ${shake ? "shake" : ""}`}>
                                <input
                                    id="typed-word"
                                    disabled={!myTurn || ended}
                                    className="form-control shadow-sm"
                                    placeholder={myTurn ? "Type your chain word…" : "Relax, watch the chain"}
                                    value={input}
                                    onChange={(e) => {
                                        const v = e.target.value;
                                        setInput(v);
                                        if (myTurn) onType(meSeat, v);
                                    }}
                                    onKeyDown={(e) => {
                                        if (e.key === "Enter") {
                                            attemptSubmit();
                                        }
                                    }}
                                />
                                <button className="btn btn-primary" type="button" disabled={!canSubmit} onClick={attemptSubmit}>
                                    Submit
                                </button>
                            </div>
                            <div className="d-flex justify-content-between align-items-center mt-2 text-muted small">
                                <span>
                                    {invalidReason
                                        ? invalidReason
                                        : myTurn
                                            ? "Press Enter or hit Submit once the chain checks out."
                                            : "Stay tuned — you’re up soon."}
                                </span>
                                <span className="badge text-bg-light">{turn.freeStart ? "Free start" : "Chain play"}</span>
                            </div>
                        </div>
                    </div>
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

function TurnTimer({ startedAt, dueAt }: { startedAt: string; dueAt: string }) {
    const [ms, setMs] = useState<number>(() => Math.max(0, new Date(dueAt).getTime() - Date.now()));
    const totalMs = Math.max(1, new Date(dueAt).getTime() - new Date(startedAt).getTime());
    useEffect(() => {
        const id = setInterval(() => setMs(Math.max(0, new Date(dueAt).getTime() - Date.now())), 100);
        return () => clearInterval(id);
    }, [dueAt]);
    const s = (ms / 1000).toFixed(1);
    const progress = Math.min(100, Math.max(0, ((totalMs - ms) / totalMs) * 100));
    const danger = ms < 4000;
    return (
        <div className={`alert ${danger ? "alert-warning" : "alert-secondary"} mb-0`} aria-live="polite" role="status">
            <div className="d-flex align-items-center gap-3">
                <div className="flex-1">
                    Time left: <strong>{s}s</strong>
                    <div className="progress mt-2" style={{ height: 10 }} aria-label="Turn timer">
                        <div
                            className={`progress-bar ${danger ? "bg-danger" : "bg-primary"}`}
                            style={{ width: `${progress}%`, transition: "width 120ms linear" }}
                            aria-valuenow={progress}
                            aria-valuemin={0}
                            aria-valuemax={100}
                        />
                    </div>
                </div>
                <span className="badge text-bg-dark">{Math.ceil(totalMs / 1000)}s limit</span>
            </div>
        </div>
    );
}

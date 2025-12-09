import { useEffect, useRef, useState } from "react";
import type { EffectState, GameState, TurnHistoryState, TurnState } from "@/api/types";
import EffectChip from "./pieces/EffectChip";
import RequiredLengthGauge from "./pieces/RequiredLengthGauge";
import PlayerSidebar from "./PlayerSidebar";
import RestrictionTrack from "./pieces/RestrictionTrack";
import { EffectType } from "./pieces/effectTypes";
import WordPreview from "./pieces/WordPreview";
import RecentWordHistory from "./pieces/RecentWordHistory";
import { computeReverseMatchLength, normalizeWord, reverseString } from "@/utils/wordMatching";

type LiveGameProps = {
    state: GameState;
    meSeat: number;
    typedBySeat: Record<number, string>;
    onType: (seat: number, word: string) => void;
    onSubmit: (word: string) => void;
    submitError?: { id: number; reason: string } | null;
    onLeave: () => void;
    canLeave?: boolean;
    ended: boolean;
    currentTurn: TurnState | null;
    wordHistory: TurnHistoryState[];
};
type EffectEvent = EffectState & { id: number };
export default function LiveGame({
    state,
    meSeat,
    typedBySeat,
    onType,
    onSubmit,
    submitError,
    onLeave,
    canLeave = true,
    ended,
    currentTurn,
    wordHistory,
}: LiveGameProps) {

    const turn: TurnState | null = currentTurn;
    const players = state.players;
    const [input, setInput] = useState("");
    const [invalidReason, setInvalidReason] = useState<string | null>(null);
    const [shake, setShake] = useState(false);
    const [seenWords, setSeenWords] = useState<Set<string>>(new Set());
    const [effectEvents, setEffectEvents] = useState<EffectEvent[]>([]);
    const lastMatchRef = useRef(0);
    const lastTurnIdRef = useRef<string | null>(null);
    const lastEffectCountRef = useRef(0);
    const audioRef = useRef<AudioContext | null>(null);

    useEffect(() => {
        const nextHistory = new Set<string>();
        players.forEach((p) => {
            if (p.lastWord) nextHistory.add(normalizeWord(p.lastWord));
        });
        setSeenWords(nextHistory);
        setInput("");
        setInvalidReason(null);
        setShake(false);
        lastMatchRef.current = 0;
    }, [players, state.currentRound?.roundId, turn?.turnId]);

    useEffect(() => {
        if (!submitError) return;
        setInvalidReason(submitError.reason);
        setShake(true);
        const id = window.setTimeout(() => setShake(false), 350);
        return () => clearTimeout(id);
    }, [submitError]);

    useEffect(() => {
        if (!turn) {
            setEffectEvents([]);
            lastEffectCountRef.current = 0;
            lastTurnIdRef.current = null;
            return;
        }

        const sameTurn = lastTurnIdRef.current === turn.turnId;
        const baseline = sameTurn ? lastEffectCountRef.current : 0;
        const newEffects = turn.effects.slice(baseline);

        newEffects.forEach((effect, idx) => {
            const id = Number(`${Date.now()}${idx}`);
            setEffectEvents((prev) => [...prev, { ...effect, id }]);
            setTimeout(() => {
                setEffectEvents((prev) => prev.filter((e) => e.id !== id));
            }, 2000);
        });

        lastTurnIdRef.current = turn.turnId;
        lastEffectCountRef.current = turn.effects.length;
    }, [turn]);

    const currentPlayer = players.find((p) => p.seat === turn?.seat);
    const previousWord = state.lastWord ?? currentPlayer?.lastWord ?? "";
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

    const myTurn = turn?.seat === meSeat;
    const minLen = turn?.minLen ?? 0;
    const roundIndex = (state.currentRound?.index ?? 0) + 1;
    if (!turn) {
        return (
            <section className="game-layout">
                <div className="game-panel">
                    <div className="d-flex flex-wrap justify-content-between align-items-start gap-3">
                        <div>
                            <p className="text-uppercase text-muted small mb-1">Round {roundIndex}</p>
                            <h5 className="card-title mb-2">Preparing next turn…</h5>
                            <p className="text-muted mb-0">Be ready - the countdown is almost done.</p>
                        </div>
                        <button type="button" className="btn btn-outline-danger" onClick={onLeave} disabled={!canLeave}>
                            Leave game
                        </button>
                    </div>
                    <div className="mt-4">
                        <div className="progress" aria-label="Loading turn">
                            <div className="progress-bar progress-bar-striped progress-bar-animated" style={{ width: "75%" }} />
                        </div>
                    </div>
                </div>
                <PlayerSidebar players={players} currentSeat={state.nextSeat} meSeat={meSeat} />
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
        if (seenWords.has(normalized)) return "That word already appeared this match.";
        return null;
    };

    const canSubmit = !ended && validateWord(input) === null;

    useEffect(() => {
        if (!invalidReason) return;
        const reason = validateWord(input);
        if (!reason) {
            setInvalidReason(null);
            setShake(false);
        }
    }, [input, invalidReason, turn, minLen, myTurn, requiredStart, reverseMatchLength, reversedPrevious, seenWords]);

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
        setSeenWords((prev) => new Set(prev).add(normalizeWord(trimmed)));
        setInput("");
        setInvalidReason(null);
        setShake(false);
        lastMatchRef.current = 0;
    };

    const activeTyped = typedBySeat[turn.seat] ?? (myTurn ? input : "");
    const totalLettersNeeded = minLen;
    const timerEffects = effectEvents.filter((e) => e.type === EffectType.ADD_TIME);
    const lengthEffects = effectEvents.filter((e) => e.type === EffectType.ADJ_MIN_LEN);
    const freeStartEffects = effectEvents.filter((e) => e.type === EffectType.FREE_START);

    if (ended) {
        const winner = [...players].sort((a, b) => b.roundWins - a.roundWins)[0];
        return (
            <section className="game-layout">
                <div className="game-panel text-center">
                    <p className="text-uppercase text-muted tracking-wide mb-2">Match finished</p>
                    <h2 className="display-6 fw-bold mb-3">{winner ? `${winner.name || "Player"} leads the lobby!` : "Game over"}</h2>
                    <p className="lead mb-4">Kick back or jump into another lobby — everyone sees their final standings on the right.</p>
                    <button type="button" className="btn btn-lg btn-primary px-4" onClick={onLeave}>
                        Back to lobby
                    </button>
                </div>
                <PlayerSidebar players={players} currentSeat={turn.seat} meSeat={meSeat} />
            </section>
        );
    }
    return (
        <section className="game-layout">
            <div className="game-panel" data-sound-turn={myTurn ? "active" : undefined}>

                <div className="d-flex flex-wrap justify-content-between align-items-start gap-3">
                    <div>
                        <p className="text-uppercase text-muted small mb-1">Round {roundIndex}</p>
                        <h4 className="mb-0">Turn #{turn.index + 1} · Seat {turn.seat}</h4>
                    </div>
                    <div className="d-flex gap-2 align-items-center position-relative">
                        <span className={`badge text-bg-primary ${lengthEffects.length ? "length-pulse" : ""}`}>
                            Need {totalLettersNeeded} letters
                        </span>
                        {lengthEffects.length > 0 && (
                            <div className="inline-effect-chip" aria-live="polite">
                                <EffectChip effect={lengthEffects[lengthEffects.length - 1]} subtle />
                            </div>
                        )}
                        <button type="button" className="btn btn-outline-danger" onClick={onLeave} disabled={!canLeave}>
                            Leave game
                        </button>
                    </div>
                </div>

                <div className="d-flex flex-column gap-4 mt-4">
                    <div className="typing-banner" aria-live="polite">
                        <div className="d-flex gap-3 align-items-center">
                            <img
                                src={currentPlayer?.iconUrl || "/avatar.svg"}
                                className="rounded-circle border flex-shrink-0"
                                width={56}
                                height={56}
                                alt={currentPlayer?.name || "Player avatar"}
                            />
                            <div className="flex-1">
                                <div className="d-flex flex-wrap align-items-center gap-2">
                                    <div className="fw-semibold leading-tight">{currentPlayer?.name || `Seat ${turn.seat}`}</div>
                                    {myTurn && <span className="badge text-bg-info text-uppercase">You</span>}
                                    {!currentPlayer?.isConnected && (
                                        <span className="badge text-bg-danger d-inline-flex align-items-center gap-1">
                                            Offline
                                        </span>
                                    )}
                                </div>
                                <div className="text-xs text-muted">Seat {turn.seat} · Wins: {currentPlayer?.roundWins ?? 0}</div>
                                <div className="mt-2">
                                    <WordPreview
                                        word={activeTyped || ""}
                                        previousWord={previousWord || ""}
                                        minLen={minLen}
                                        reverseMatchLength={reverseMatchLength}
                                        label="Current typing preview"
                                    />
                                </div>
                            </div>
                            <span className="badge text-bg-primary">Current turn</span>
                        </div>
                    </div>
                    <TurnTimer startedAt={turn.startedAt} dueAt={turn.dueAt} effects={timerEffects} />
                    <RestrictionTrack
                        minLen={minLen}
                        typedWord={activeTyped || ""}
                        previousWord={previousWord || ""}
                        startLetter={requiredStart}
                        freeStart={turn.freeStart}
                        reverseMatchLength={reverseMatchLength}
                        invalid={shake}
                        requiredWords={totalLettersNeeded}
                    />
                    <RecentWordHistory history={wordHistory} fallbackPrevious={previousWord || ""} />
                    <div className="flex flex-wrap gap-4 align-items-center">
                        <RequiredLengthGauge value={(activeTyped || "").length} min={minLen} />
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
                                <span className={`badge text-bg-light ${freeStartEffects.length ? "free-flash" : ""}`}>
                                    {turn.freeStart ? "Free start" : "Chain play"}
                                </span>
                                {freeStartEffects.length > 0 && (
                                    <EffectChip effect={freeStartEffects[freeStartEffects.length - 1]} subtle />
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}

function TurnTimer({
    startedAt,
    dueAt,
    effects,
}: {
    startedAt: string;
    dueAt: string;
    effects: EffectEvent[];
}) {    const [ms, setMs] = useState<number>(() => Math.max(0, new Date(dueAt).getTime() - Date.now()));
    const totalMs = Math.max(1, new Date(dueAt).getTime() - new Date(startedAt).getTime());
    useEffect(() => {
        const id = setInterval(() => setMs(Math.max(0, new Date(dueAt).getTime() - Date.now())), 100);
        return () => clearInterval(id);
    }, [dueAt]);
    const s = (ms / 1000).toFixed(1);
    const progress = Math.min(100, Math.max(0, ((totalMs - ms) / totalMs) * 100));
    const danger = ms < 4000;
    return (
        <div
            className={`alert ${danger ? "alert-warning" : "alert-secondary"} mb-0 position-relative`}
            aria-live="polite"
            role="status"
            style={{ overflow: "visible" }}
        >
            <div className="timer-effect-stack" aria-hidden={effects.length === 0}>
                {effects.map((effect) => (
                    <EffectChip key={effect.id} effect={effect} floating />
                ))}
            </div>            <div className="d-flex align-items-center gap-3">
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

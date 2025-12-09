import { useCallback, useEffect, useRef, useState } from "react";
import type { EffectState, GameState, TurnHistoryState, TurnState } from "@/api/types";
import EffectChip from "./pieces/EffectChip";
import RequiredLengthGauge from "./pieces/RequiredLengthGauge";
import PlayerSidebar from "./PlayerSidebar";
import RestrictionTrack from "./pieces/RestrictionTrack";
import { EffectType } from "./pieces/effectTypes";
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
    const validateWord = useCallback(
        (word: string): string | null => {
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
        },
        [myTurn, minLen, requiredStart, reversedPrevious, reverseMatchLength, seenWords, turn?.freeStart]
    );

    const canSubmit = !ended && validateWord(input) === null;

    useEffect(() => {
        if (!invalidReason) return;
        const reason = validateWord(input);
        if (!reason) {
            setInvalidReason(null);
            setShake(false);
        }
    }, [input, invalidReason, validateWord]);

    const attemptSubmit = useCallback(() => {
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
    }, [input, onSubmit, validateWord]);

    useEffect(() => {
        if (!myTurn) return;
        onType(meSeat, input);
    }, [input, meSeat, myTurn, onType]);

    useEffect(() => {
        if (!myTurn || ended) return;

        const handleKeyDown = (event: KeyboardEvent) => {
            const target = event.target as HTMLElement;
            if (
                target?.tagName.toLowerCase() === "input" ||
                target?.tagName.toLowerCase() === "textarea" ||
                target?.isContentEditable
            ) {
                return;
            }

            if (event.key === "Enter") {
                event.preventDefault();
                attemptSubmit();
                return;
            }

            if (event.key === "Backspace") {
                event.preventDefault();
                setInput((prev) => prev.slice(0, -1));
                return;
            }

            if (/^[a-z]$/i.test(event.key)) {
                event.preventDefault();
                setInput((prev) => `${prev}${event.key}`);
            }
        };

        window.addEventListener("keydown", handleKeyDown);
        return () => window.removeEventListener("keydown", handleKeyDown);
    }, [attemptSubmit, ended, myTurn]);

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

    const activeTyped = typedBySeat[turn.seat] ?? (myTurn ? input : "");
    const totalLettersNeeded = minLen;
    const timerEffects = effectEvents.filter((e) => e.type === EffectType.ADD_TIME);
    const turnContext = turn
        ? {
            round: roundIndex,
            turn: turn.index + 1,
            seat: turn.seat,
            playerName: currentPlayer?.name ?? null,
            requiredLength: totalLettersNeeded,
            startLetter: requiredStart,
            freeStart: turn.freeStart,
            wins: currentPlayer?.roundWins ?? 0,
        }
        : null;

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
                <PlayerSidebar players={players} currentSeat={turn.seat} meSeat={meSeat} turnContext={turnContext ?? undefined} />
            </section>
        );
    }
    return (
        <section className="game-layout">
            <div className="game-panel" data-sound-turn={myTurn ? "active" : undefined}>

                <div className="d-flex flex-wrap justify-content-between align-items-start gap-3">
                    <div className="d-flex flex-column gap-1">
                        <p className="text-uppercase text-muted small mb-0">Round {roundIndex}</p>
                        <h4 className="mb-0">Turn #{turn.index + 1}</h4>
                        {!currentPlayer?.isConnected && <span className="text-danger small">Offline</span>}
                    </div>
                    <div className="d-flex gap-2 align-items-center position-relative">
                        <button type="button" className="btn btn-outline-danger" onClick={onLeave} disabled={!canLeave}>
                            Leave game
                        </button>
                    </div>
                </div>
                <div className="d-flex flex-row justify-content-between">
                    <TurnTimer startedAt={turn.startedAt} dueAt={turn.dueAt} effects={timerEffects} />
                    <RequiredLengthGauge value={(activeTyped || "").length} min={minLen} />
                </div>
                <div className="d-flex flex-column gap-4 mt-4">

                    <RestrictionTrack
                        minLen={minLen}
                        typedWord={activeTyped || ""}
                        previousWord={previousWord || ""}
                        startLetter={requiredStart}
                        freeStart={turn.freeStart}
                        reverseMatchLength={reverseMatchLength}
                        invalid={shake}
                        requiredWords={totalLettersNeeded}
                    >
                        <span>
                            {invalidReason
                                ?? myTurn
                                    ? "Type letters anywhere on the page. Backspace deletes; Enter submits."
                                    : "Stay tuned — you’re up soon."}
                        </span>
                        <RecentWordHistory history={wordHistory} fallbackPrevious={previousWord || ""} />
                    </RestrictionTrack>
                </div>
            </div>
            <PlayerSidebar players={players} currentSeat={turn.seat} meSeat={meSeat} turnContext={turnContext ?? undefined} />
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
            className={`alert ${danger ? "alert-warning" : "alert-secondary"} mb-0 position-relative w-100`}
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
            </div>
        </div>
    );
}

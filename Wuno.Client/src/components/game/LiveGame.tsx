import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { GameState, TurnHistoryState, TurnState } from "@/api/types";
import RequiredLengthGauge from "./pieces/RequiredLengthGauge";
import PlayerSidebar from "./PlayerSidebar";
import RestrictionTrack from "./pieces/RestrictionTrack";
import RecentWordHistory from "./pieces/RecentWordHistory";
import { computeReverseMatchLength, normalizeWord, reverseString } from "@/utils/wordMatching";
import TurnTimer from "./pieces/TurnTimer";
import wordListText from "@/assets/words.txt?raw";
import { playErrorSound, playSuccessSound, playTurnStartSound, startTickingSound, stopTickingSound, playMatchSound, playExplosionSound } from "@/utils/sounds";
import { getLetterValue } from "@/utils/letterScoring";
import { TIME_BONUS_MULTIPLIER, SCORE_DIVISOR } from "@/constants";

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
    const lastMatchRef = useRef(0);
    const prevTurnIdRef = useRef<string | null>(null);
    const pendingSubmitRef = useRef(false);  // Track if we just submitted
    const mobileInputRef = useRef<HTMLInputElement>(null);
    const [isMobile, setIsMobile] = useState(false);

    // Detect mobile device
    useEffect(() => {
        const checkMobile = () => {
            const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;
            const isMobileWidth = window.innerWidth < 768;
            setIsMobile(isTouchDevice || isMobileWidth);
        };
        checkMobile();
        window.addEventListener('resize', checkMobile);
        return () => window.removeEventListener('resize', checkMobile);
    }, []);

    const dictionary = useMemo(
        () => new Set(wordListText.split(/\r?\n/).map(normalizeWord).filter(Boolean)),
        []
    );

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
        // Server rejected our word - play error sound and clear pending state
        pendingSubmitRef.current = false;
        playErrorSound();
        setInvalidReason(submitError.reason);
        setShake(true);
        const id = window.setTimeout(() => setShake(false), 350);
        return () => clearTimeout(id);
    }, [submitError]);

    const currentPlayer = players.find((p) => p.seat === turn?.seat);
    const previousWord = state.lastWord ?? "";
    const reversedPrevious = reverseString(normalizeWord(previousWord));
    const normalizedInput = normalizeWord(input);
    const reverseMatchLength = computeReverseMatchLength(normalizedInput, reversedPrevious);
    const requiredStart = previousWord ? previousWord.slice(-1) : null;
    const myTurn = turn?.seat === meSeat;
    const minLen = turn?.minLen ?? 0;
    const roundIndex = (state.currentRound?.index ?? 0) + 1;

    // Calculate potential score and bonus time from current word
    const potentialScore = useMemo(() => {
        if (!normalizedInput || reverseMatchLength === 0) return 0;
        let score = 0;
        for (let i = 0; i < reverseMatchLength && i < normalizedInput.length; i++) {
            score += getLetterValue(normalizedInput[i]) * (i + 1);
        }
        return score;
    }, [normalizedInput, reverseMatchLength]);

    // Bonus time = score * TIME_BONUS_MULTIPLIER (same as backend logic)
    const bonusSeconds = potentialScore * TIME_BONUS_MULTIPLIER;
    // Min length reduction = score / SCORE_DIVISOR
    const potentialMinLenReduction = Math.floor(potentialScore / SCORE_DIVISOR);

    // Play turn start sound and start ticking when it's my turn
    useEffect(() => {
        if (ended || !turn) {
            stopTickingSound();
            prevTurnIdRef.current = null;
            return;
        }
        
        const isNewTurn = prevTurnIdRef.current !== turn.turnId;
        prevTurnIdRef.current = turn.turnId;
        
        if (myTurn && isNewTurn) {
            playTurnStartSound();
            startTickingSound();
            // Auto-focus mobile input to trigger keyboard
            if (isMobile && mobileInputRef.current) {
                // Small delay to ensure the UI has rendered
                setTimeout(() => mobileInputRef.current?.focus(), 100);
            }
        } else if (!myTurn) {
            stopTickingSound();
            // Blur mobile input when not our turn
            if (mobileInputRef.current) {
                mobileInputRef.current.blur();
            }
        }
        
        return () => stopTickingSound();
    }, [turn?.turnId, myTurn, ended, isMobile]);

    // Play match sound when reverse match length changes
    useEffect(() => {
        if (!turn || reverseMatchLength <= lastMatchRef.current) return;
        playMatchSound(reverseMatchLength);
        lastMatchRef.current = reverseMatchLength;
    }, [reverseMatchLength, turn]);

    // Track previous turn owner for timeout detection
    const wasMyTurnRef = useRef(false);

    // Play success/error sound when turn advances away from us
    useEffect(() => {
        // Check if turn moved away from us (was ours, now isn't)
        if (wasMyTurnRef.current && !myTurn && turn) {
            if (pendingSubmitRef.current) {
                // We submitted and it was accepted
                playSuccessSound();
            } else {
                // We didn't submit in time (timeout) - bomb explodes!
                playExplosionSound();
            }
            pendingSubmitRef.current = false;
        }
        // Update ref AFTER checking (so next render sees old value)
        wasMyTurnRef.current = myTurn;
    }, [turn?.turnId, myTurn]);
    const validateWord = useCallback(
        (word: string): string | null => {
            const trimmed = word.trim();
            if (!myTurn) return "Wait for your spotlight.";
            if (!trimmed) return "Type a word to submit.";
            const normalized = normalizeWord(trimmed);
            if (!/^[a-z]+$/i.test(trimmed)) return "Letters only, no symbols.";
            if (normalized.length < minLen) return `Need at least ${minLen} letters.`;
            if (requiredStart && normalized[0] !== normalizeWord(requiredStart)[0]) {
                return `Must start with '${requiredStart.toUpperCase()}'`;
            }
            if (!dictionary.has(normalized)) return "Not in the allowed word list.";
            if (seenWords.has(normalized)) return "That word already appeared this match.";
            return null;
        },
        [dictionary, myTurn, minLen, requiredStart, seenWords]
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
            playErrorSound();
            setTimeout(() => setShake(false), 350);
            return;
        }
        const trimmed = input.trim();
        if (!trimmed) return;
        // Mark that we're waiting for server confirmation
        pendingSubmitRef.current = true;
        onSubmit(trimmed);
        setInput("");
        setInvalidReason(null);
        setShake(false);
        lastMatchRef.current = 0;
    }, [input, onSubmit, validateWord]);

    useEffect(() => {
        if (!myTurn) return;
        onType(meSeat, input);
    }, [input, meSeat, myTurn, onType]);

    // Handle input change from mobile text field
    const handleMobileInput = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        if (!myTurn || ended) return;
        
        const newValue = e.target.value.toLowerCase().replace(/[^a-z]/g, '');
        
        // Block wrong first letter
        if (newValue.length === 1 && requiredStart) {
            const requiredLower = normalizeWord(requiredStart).toLowerCase();
            if (newValue !== requiredLower) {
                playErrorSound();
                setShake(true);
                setTimeout(() => setShake(false), 350);
                e.target.value = '';  // Clear invalid first letter
                return;
            }
        }
        
        setInput(newValue);
    }, [myTurn, ended, requiredStart]);

    // Handle Enter key on mobile input
    const handleMobileKeyDown = useCallback((e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            attemptSubmit();
        }
    }, [attemptSubmit]);

    // Desktop keyboard listener (only active when mobile input not focused)
    useEffect(() => {
        if (!myTurn || ended) return;

        const handleKeyDown = (event: KeyboardEvent) => {
            const target = event.target as HTMLElement;
            // Allow our mobile input, block other inputs
            if (
                (target?.tagName.toLowerCase() === "input" && target !== mobileInputRef.current) ||
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
                const newLetter = event.key.toLowerCase();
                
                // Block wrong first letter
                if (input.length === 0 && requiredStart) {
                    const requiredLower = normalizeWord(requiredStart).toLowerCase();
                    if (newLetter !== requiredLower) {
                        playErrorSound();
                        setShake(true);
                        setTimeout(() => setShake(false), 350);
                        return;
                    }
                }
                
                setInput((prev) => `${prev}${event.key}`);
            }
        };

        window.addEventListener("keydown", handleKeyDown);
        return () => window.removeEventListener("keydown", handleKeyDown);
    }, [attemptSubmit, ended, myTurn, input, requiredStart]);


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
    const turnContext = turn
        ? {
            round: roundIndex,
            turn: turn.index + 1,
            playerName: currentPlayer?.name ?? null,
            requiredLength: totalLettersNeeded,
            startLetter: requiredStart,
            wins: currentPlayer?.roundWins ?? 0,
        }
        : null;

    const top = (() => {
        const arr = [...players]
            .sort((a, b) => b.roundWins - a.roundWins)
            .slice(0, 3);
        [arr[0], arr[1]] = [arr[1], arr[0]];
        return arr;
    })();

    if (ended) {
        const winner = [...players].sort((a, b) => b.roundWins - a.roundWins)[0];
        return (
            <section className="game-layout">
                <div className="game-panel text-center">
                    <p className="text-uppercase text-muted tracking-wide mb-2">Match finished</p>
                    <h2 className="display-6 fw-bold mb-3">{winner ? `${winner.name || "Player"} Wins!` : "Game over"}</h2>
                    <div className="card-header">
                        <h5 className="card-title mb-0">Leaderboard</h5>
                    </div>
                    <div className="card-body m-2">
                        <div className="grid grid-cols-3 gap-3 items-end text-center">
                            {Array.from({ length: 3 }).map((_, i) => {
                                let p = top[i];
                                const podiumH = [24, 32, 20][i]; // mid tallest
                                const podiumColors = ["bg-gray-300", "bg-amber-400", "bg-amber-600"];
                                return (
                                    <div key={i} className="flex flex-col items-center">
                                        <div className="text-sm mb-2">{p?.name ?? "—"}</div>
                                        <div className={`w-full bg-base-200 border rounded-t-xl flex items-end justify-center ${podiumColors[i]}`}
                                            style={{ height: `${podiumH}vh` }}>
                                            <div className="mb-2 text-2xl font-bold">{p ? p.roundWins : ""}</div>
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                    <button type="button" className="btn btn-lg btn-primary px-4" onClick={() => window.location.href = "/lobby"}>
                        Back to lobby
                    </button>
                </div>
                <PlayerSidebar players={players} currentSeat={turn.seat} meSeat={meSeat} turnDueAt={turn.dueAt} turnContext={turnContext ?? undefined} />
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
                <div className="d-flex flex-row justify-content-between gap-3">
                    <TurnTimer 
                        startedAt={turn.startedAt} 
                        dueAt={turn.dueAt} 
                        bonusSeconds={myTurn ? bonusSeconds : 0}
                        potentialScore={myTurn ? potentialScore : 0}
                    />
                    <RequiredLengthGauge 
                        value={(activeTyped || "").length} 
                        min={minLen} 
                        potentialMinLen={myTurn ? Math.max(0, minLen - potentialMinLenReduction) : minLen}
                    />
                </div>
                <div className="d-flex flex-column gap-4 mt-4">

                    <RestrictionTrack
                        minLen={minLen}
                        typedWord={activeTyped || ""}
                        previousWord={previousWord || ""}
                        startLetter={requiredStart}
                        reverseMatchLength={reverseMatchLength}
                        invalid={shake}
                        requiredWords={totalLettersNeeded}
                    >
                        {invalidReason ? (
                            <div className="invalid-reason">{invalidReason}</div>
                        ) : (
                            <span className="text-muted">
                                {myTurn
                                    ? (isMobile ? "Tap below to type" : "Type letters anywhere on the page")
                                    : `${turnContext?.playerName ?? "Another player"} is typing`}
                            </span>
                        )}
                        <RecentWordHistory history={wordHistory} fallbackPrevious={previousWord || ""} players={players} />
                    </RestrictionTrack>

                    {/* Mobile input - visible on mobile during user's turn */}
                    {myTurn && (
                        <div className="mobile-input-container d-flex gap-2">
                            <input
                                ref={mobileInputRef}
                                type="text"
                                className="form-control mobile-word-input"
                                placeholder={requiredStart ? `Type a word starting with "${requiredStart.toUpperCase()}"` : "Type your word..."}
                                value={input}
                                onChange={handleMobileInput}
                                onKeyDown={handleMobileKeyDown}
                                autoComplete="off"
                                autoCapitalize="off"
                                autoCorrect="off"
                                spellCheck={false}
                                enterKeyHint="send"
                                disabled={!myTurn || ended}
                                aria-label="Word input"
                            />
                            <button 
                                type="button" 
                                className="btn btn-primary submit-word-btn"
                                onClick={attemptSubmit}
                                disabled={!canSubmit}
                            >
                                Submit
                            </button>
                        </div>
                    )}
                </div>
            </div>
            <PlayerSidebar players={players} currentSeat={turn.seat} meSeat={meSeat} turnDueAt={turn.dueAt} turnContext={turnContext ?? undefined} />
        </section>
    );
}
import { useEffect, useMemo, useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useUser } from "@/context/UserContext";
import type { GameState, JoinGameResponse, PlayerState, TurnState } from "@/api/types";
import { getCookie } from "@/auth/cookies";
import WaitingRoom from "@/components/game/WaitingRoom";
import RoundStart from "@/components/game/RoundStart";
import LiveGame from "@/components/game/LiveGame";
import { createGameHub } from "@/hub/connection";
import { setPendingJoin } from "@/utils/pendingJoin";
import { useToast } from "@/context/ToastContext";

type Phase = "waiting" | "round-start" | "playing" | "ended";

export default function GamePage() {
    const { code } = useParams<{ code: string }>();
    const nav = useNavigate();
    const { user } = useUser();
    const hubRef = useRef<ReturnType<typeof createGameHub> | null>(null);

    const [mePlayerId, setMePlayerId] = useState<string | null>(null);
    const [meSeat, setMeSeat] = useState<number | null>(null);
    const [state, setState] = useState<GameState | null>(null);
    const [phase, setPhase] = useState<Phase>("waiting");
    const [roundCountdownMs, setRoundCountdownMs] = useState<number | null>(null);
    const [typedBySeat, setTypedBySeat] = useState<Record<number, string>>({});
    const [effectsFlash, setEffectsFlash] = useState<string[]>([]); // derived effect chips
    const { push } = useToast();

    // For “diffing” turn changes → create animated effect chips
    const lastTurnRef = useRef<TurnState | null>(null);

    const myUserId = useMemo(() => {
        return (user?.userId as string | undefined) || getCookie() || "";
    }, [user]);
    const leaveGame = async () => {
        if (!state || !hubRef.current) {
            nav("/lobby", { replace: true });
            return;
        }
        const ok = window.confirm("Leave this game?");
        if (!ok) return;

        try {
            await hubRef.current.invoke("LeaveGame", state.gameId);
        } catch (e) {
            console.error("LeaveGame failed:", e);
            // even if hub call fails, we still tear down & navigate so the UI is consistent
        } finally {
            try { await hubRef.current.stop(); } catch { }
            nav("/lobby", { replace: true });
        }
    };
    useEffect(() => {
        const beforeUnload = () => {
            // best-effort: no await (unload), but hub stop is fine
            try { hubRef.current?.invoke("LeaveGame", state?.gameId); } catch { }
            try { hubRef.current?.stop(); } catch { }
        };
        window.addEventListener("beforeunload", beforeUnload);
        return () => window.removeEventListener("beforeunload", beforeUnload);
    }, [state?.gameId]);

    // Connect + join on mount
    useEffect(() => {
    let cancelled = false;

    (async () => {
        try {
            if (!code) { nav("/", { replace: true }); return; }
            if (!myUserId) {
                setPendingJoin(code);
                nav("/", { replace: true });
                return;
            }

        const hub = createGameHub();
        hubRef.current = hub;

        // Wire up server → client events
        hub.on("ConnectedToGame", (res: JoinGameResponse) => {
            if (cancelled) return;
            setMePlayerId(res.playerId);
            setState(res.state);
            const seat = res.state.players.find(p => p.playerId === res.playerId)?.seat ?? null;
            setMeSeat(seat);
            setPhase(res.state.status === 0 ? "waiting" : "playing"); // GameStatus.WAITING==0, ACTIVE==1, FINISHED==2
        });

            hub.on("ConnectionFailed", (msg: string) => {
                if (!cancelled) {
                    push(msg || "Failed to connect to game.");
                    nav("/lobby", { replace: true });
                }
        });

        hub.on("PlayersUpdated", (players: PlayerState[]) => {
            if (cancelled) return;
            setState(s => (s ? { ...s, players } : s));
        });

        hub.on("AllPlayersReady", (ms: number) => {
            if (cancelled) return;
            setRoundCountdownMs(ms);
            setPhase("round-start");
        });

        hub.on("MatchStarted", (g: GameState) => {
            if (cancelled) return;
            setState(g);
            setPhase("playing");
        });

        hub.on("GameUpdated", (g: GameState) => {
            if (cancelled) return;
            // derive simple effect chips based on previous vs new currentTurn
            deriveEffectChips(lastTurnRef.current, g.currentTurn, setEffectsFlash);
            lastTurnRef.current = g.currentTurn;
            setState(g);
        });

        hub.on("NewRoundStarted", (g: GameState) => {
            if (cancelled) return;
            setTypedBySeat({});
            setEffectsFlash([]);
            setState(g);
            setPhase("playing");
        });

        hub.on("RoundEnded", (g: GameState) => {
            if (cancelled) return;
            setState(g);
            setPhase("round-start");
            setRoundCountdownMs(3000); // UX: short pause until NewRoundStarted
        });

        hub.on("MatchEnded", (g: GameState) => {
            if (cancelled) return;
            setState(g);
            setPhase("ended");
        });

            hub.on("WordChanged", (word: string) => {
                setState(s => {
                    if (!s) return s;
                    const seat = s.currentTurn?.seat;
                    if (seat == null) return s;
                    setTypedBySeat(prev => ({ ...prev, [seat]: word }));
                    return s;
                });
            });

        hub.on("error", (err: string) => {
            console.error("Hub error:", err);
        });

        await hub.start();
        console.log("Connecting to game (" + code + ") as ", myUserId);
        await hub.invoke("ConnectToGame", code);

        } catch (err) {
        console.error(err);
        if (!cancelled) nav("/", { replace: true });
        }
    })();

    return () => {
        cancelled = true;
        if (hubRef.current?.state === "Connected") hubRef.current.stop();
    };
    }, [code, myUserId, nav, push]);

    const mePlayer = useMemo(
    () => state?.players.find(p => p.playerId === mePlayerId) ?? null,
    [state, mePlayerId]
    );

    // Ready toggle
    const setReady = async (ready: boolean) => {
    if (!hubRef.current || !state || meSeat == null) return;
    try {
        await hubRef.current.invoke("Ready", state.gameId, ready);
    } catch (e) {
        // if not connected, redirect to lobby
        if (e instanceof Error && e.message.includes("not connected")) {
            nav("/lobby", { replace: true });
        }
    }
    };

    // Submit a word
    const submitWord = async (word: string) => {
    if (!hubRef.current || !state || meSeat == null) return;
    try {
        await hubRef.current.invoke(
        "SubmitWord",
        state.gameId,
        state.currentRound.roundId,
        state.currentTurn.turnId,
        word
        );
    } catch (e) {
        console.error(e);
    }
    };

    // Broadcast local typing
    const onLocalType = (seat: number, word: string) => {
    setTypedBySeat(prev => ({ ...prev, [seat]: word }));
    if (!hubRef.current) return;
    hubRef.current.invoke("WordChanged", word).catch(() => {});
    };

    // Countdown tick for “round-start”
    useEffect(() => {
    if (phase !== "round-start" || roundCountdownMs == null) return;
    const t = setInterval(() => {
        setRoundCountdownMs(ms => (ms != null && ms > 0 ? ms - 100 : 0));
    }, 100);
    return () => clearInterval(t);
    }, [phase, roundCountdownMs]);

    if (!state) {
        return (
            <div className="container mx-auto px-4 my-10">
                <div className="card shadow">
                    <div className="card-body">Connecting to game…</div>
                </div>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-3 py-6">
            {/* Top layout bar with code + Leave */}
            <div className="mb-4 flex items-center justify-between">
                <div className="text-sm opacity-70">
                    <span className="font-semibold">Game</span>{" "}
                    <span className="opacity-80">#{(code ?? "").toUpperCase()}</span>
                </div>
                <button
                    type="button"
                    className="btn btn-outline-danger"
                    onClick={leaveGame}
                    disabled={!hubRef.current || hubRef.current.state !== "Connected"}
                    title="Leave this game and return to the lobby"
                >
                    Leave game
                </button>
            </div>

            {/* existing phase renderers */}
            {phase === "waiting" && (
                <WaitingRoom
                    players={state.players}
                    mePlayerId={mePlayerId}
                    onReadyChange={setReady}
                />
            )}

            {phase === "round-start" && (
                <RoundStart
                    players={state.players}
                    targetWins={state.targetWins}
                    msRemaining={roundCountdownMs ?? 0}
                />
            )}

            {(phase === "playing" || phase === "ended") && state.currentTurn && (
                <LiveGame
                    state={state}
                    meSeat={meSeat ?? 0}
                    typedBySeat={typedBySeat}
                    onType={onLocalType}
                    onSubmit={submitWord}
                    effectsFlash={effectsFlash}
                    ended={phase === "ended"}
                />
            )}
        </div>
    );
}

/** Derive simple “effect chips” (for animation) by diffing consecutive turns. */
function deriveEffectChips(prev: TurnState | null, next: TurnState | null, push: (chips: string[]) => void) {
    if (!prev || !next) return;
    const chips: string[] = [];
    if (next.seat !== prev.seat) {
    // Compare constraints the *new* player got vs the previous turn we saw for any player
    if (next.durationSec > prev.durationSec) chips.push(`+${next.durationSec - prev.durationSec}s Time`);
    if (next.durationSec < prev.durationSec) chips.push(`-${prev.durationSec - next.durationSec}s Time`);
    if (next.minLen > prev.minLen) chips.push(`Opponent Min +${next.minLen - prev.minLen}`);
    if (next.minLen < prev.minLen) chips.push(`Min -${prev.minLen - next.minLen}`);
    if (next.freeStart && !prev.freeStart) chips.push("Free Start");
    }
    if (chips.length) push(chips);
}

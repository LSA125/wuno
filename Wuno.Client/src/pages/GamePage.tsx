import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useUser } from "@/context/UserContext";
import type { GameState, JoinGameResponse, PlayerState, TurnHistoryState, InGameStatsResponse } from "@/api/types";
import { getCookie } from "@/auth/cookies";
import WaitingRoom from "@/components/game/WaitingRoom";
import RoundStart from "@/components/game/RoundStart";
import LiveGame from "@/components/game/LiveGame";
import { createGameHub } from "@/hub/connection";
import { setPendingJoin } from "@/utils/pendingJoin";
import { useToast } from "@/context/ToastContext";
import { Api } from "@/api/client";

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
    const [submitError, setSubmitError] = useState<{ id: number; reason: string } | null>(null);
    const [wordHistory, setWordHistory] = useState<TurnHistoryState[]>([]);
    const [playerStats, setPlayerStats] = useState<Record<string, InGameStatsResponse>>({});
    const { push } = useToast();

    const lastRoundRef = useRef<string | null>(null);
    const gameIdRef = useRef<string | null>(null);
    const roundIdRef = useRef<string | null>(null);

    const myUserId = useMemo(() => {
        return (user?.userId as string | undefined) || getCookie() || "";
    }, [user]);

    const updateWordHistory = useCallback(
        (
            updater:
                | TurnHistoryState[]
                | null
                | undefined
                | ((prev: TurnHistoryState[]) => TurnHistoryState[] | null | undefined)
        ) => {
            const roundIdAtUpdate = roundIdRef.current;
            setWordHistory(prev => {
                const safePrev = prev ?? [];
                if (roundIdAtUpdate && roundIdRef.current !== roundIdAtUpdate) return safePrev;

                const resolved = typeof updater === "function" ? updater(safePrev) : updater;
                const scopedHistory = resolved ?? [];
                const dedupedHistory = Array.from(
                    scopedHistory
                        .reduce((map, entry) => map.set(entry.turnId, entry), new Map<string, TurnHistoryState>())
                        .values()
                );

                return dedupedHistory;
            });
        },
        []
    );

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
    const copyLink = async () => {
        if (!code) return;

        try {
            await navigator.clipboard.writeText(window.location.href);
            push("Game link copied");
        } catch (err) {
            console.error("Copy failed", err);
            push("Couldn't copy the link. Try manually copying the address bar.");
        }
    };
    const requestRecentHistory = useCallback(async () => {
        const hub = hubRef.current;
        const gameId = gameIdRef.current;
        if (!hub || !gameId) return;
        try {
            const history = await hub.invoke<TurnHistoryState[]>("RequestRecentWordHistory", gameId);
            updateWordHistory(history);
        } catch (err) {
            console.error("Recent history request failed", err);
        }
    }, [updateWordHistory]);
    useEffect(() => {
        const beforeUnload = () => {
            // Use sendBeacon for reliable delivery on tab close
            // The beacon API guarantees the browser will send this even during page unload
            if (state?.gameId && navigator.sendBeacon) {
                const API_BASE_URL = import.meta.env.VITE_API_URL || "";
                navigator.sendBeacon(`${API_BASE_URL}/api/games/leave`, "");
            }
            // Still try graceful SignalR stop (may not complete before tab closes)
            try { hubRef.current?.stop(); } catch { }
        };
        window.addEventListener("beforeunload", beforeUnload);
        return () => window.removeEventListener("beforeunload", beforeUnload);
    }, [state?.gameId]);

    // Fetch stats for all players when players list updates
    useEffect(() => {
        if (!state?.players || state.players.length === 0) return;
        
        const fetchAllStats = async () => {
            const statsPromises = state.players
                .filter(p => p.userId) // Only fetch for players with userId
                .map(async (p) => {
                    try {
                        const stats = await Api.getInGameStats(p.userId!);
                        return { playerId: p.playerId, stats };
                    } catch {
                        return null;
                    }
                });
            
            const results = await Promise.all(statsPromises);
            const newStatsMap: Record<string, InGameStatsResponse> = {};
            results.forEach(r => {
                if (r) newStatsMap[r.playerId] = r.stats;
            });
            setPlayerStats(newStatsMap);
        };
        
        fetchAllStats();
    }, [state?.players]);

    // Connect + join on mount
    useEffect(() => {
        let cancelled = false;
        setPhase("playing"); // for testing
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
            gameIdRef.current = res.state.gameId;
            lastRoundRef.current = res.state.currentRound?.roundId ?? null;
            roundIdRef.current = res.state.currentRound?.roundId ?? null;
            updateWordHistory([]);
            requestRecentHistory();
            setPhase(derivePhaseFromGame(res.state));
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

        hub.on("WordRejected", (reason: string) => {
            if (cancelled) return;
            setSubmitError({ id: Date.now(), reason: reason || "Word rejected." });
        });

        hub.on("MatchStarted", (g: GameState) => {

        });

        hub.on("GameUpdated", (g: GameState) => {
            if (cancelled) return;
            console.log("GameUpdated received", g);
            setState(g);
            setPhase(derivePhaseFromGame(g));
            if (g.status !== 0) setRoundCountdownMs(null);
            gameIdRef.current = g.gameId;
            const nextRoundId = g.currentRound?.roundId ?? null;
            if (roundIdRef.current !== nextRoundId) {
                roundIdRef.current = nextRoundId;
                updateWordHistory([]);
                requestRecentHistory();
            }
        });

        hub.on("NewRoundStarted", () => { });

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

        hub.on("RecentWordHistory", (history: TurnHistoryState[]) => {
            if (cancelled) return;
            updateWordHistory(history);
        });

        hub.on("WordHistoryAppended", (entry: TurnHistoryState) => {
            if (cancelled) return;
            updateWordHistory(prev => [...prev, entry]);
        });

        hub.on("error", (err: string) => {
            console.error("Hub error:", err);
        });

        hub.onreconnected(() => {
            requestRecentHistory();
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
    }, [code, myUserId, nav, push, requestRecentHistory]);


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
        if (!hubRef.current || !state || meSeat == null || !state.currentRound || !state.currentTurn) return;
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

    const onLocalType = useCallback(
        (seat: number, word: string) => {
            setTypedBySeat(prev => ({ ...prev, [seat]: word }));
            if (!hubRef.current) return;
            hubRef.current.invoke("WordChanged", word).catch(() => { });
        },
        []
    );

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

    const inLivePhase = phase === "playing" || phase === "ended";

    return (
        <div className="container mx-auto px-3 py-6">
            {/* Top layout bar with code + Leave */}
            <div className="mb-4 flex items-center justify-between gap-3">
                <div className="flex items-center gap-2 text-sm opacity-70">
                    <span className="font-semibold">Game</span>{" "}
                    <span className="opacity-80">#{(code ?? "").toUpperCase()}</span>
                    <button
                        type="button"
                        className="btn btn-outline-primary btn-sm"
                        onClick={copyLink}
                    >
                        Copy link
                    </button>
                </div>
                {!inLivePhase && (
                    <button
                        type="button"
                        className="btn btn-outline-danger"
                        onClick={leaveGame}
                        disabled={!hubRef.current || hubRef.current.state !== "Connected"}
                        title="Leave this game and return to the lobby"
                    >
                        Leave game
                    </button>
                )}
            </div>

            {/* existing phase renderers */}
            {phase === "waiting" && (
                <WaitingRoom
                    players={state.players}
                    mePlayerId={mePlayerId}
                    onReadyChange={setReady}
                    playerStats={playerStats}
                />
            )}

            {phase === "round-start" && (
                <RoundStart
                    players={state.players}
                    targetWins={state.targetWins}
                    msRemaining={roundCountdownMs ?? 0}
                    playerStats={playerStats}
                />
            )}

            {inLivePhase && (
                <LiveGame
                    state={state}
                    meSeat={meSeat ?? 0}
                    typedBySeat={typedBySeat}
                    onType={onLocalType}
                    onSubmit={submitWord}
                    submitError={submitError}
                    onLeave={leaveGame}
                    canLeave={!!hubRef.current && hubRef.current.state === "Connected"}
                    ended={phase === "ended"}
                    currentTurn={state.currentTurn ?? null}
                    wordHistory={wordHistory}
                />
            )}
        </div>
    );
}

function derivePhaseFromGame(g: GameState): Phase {
    if (g.status === 2) return "ended"; // GameStatus.FINISHED
    if (g.status === 0) return "waiting"; // GameStatus.WAITING
    if (g.currentTurn || g.currentRound) return "playing";
    return "round-start";
}
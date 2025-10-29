import React, { useEffect, useMemo, useState } from "react";
import Button from "@/components/ui/Button";
import Card from "@/components/ui/Card";
import { Api } from "@/api/client";
import { GameState, JoinGameResponse, NewGameResponse } from "@/api/types";
import { createGameHub } from "@/hub/connection";
import { getCookie } from "@/auth/cookies";

export default function GamePanel({ onToast, onRequireTempUser }: { onToast: (s: string) => void; onRequireTempUser: () => Promise<string> }) {
    const [playerCount, setPlayerCount] = useState(2);
    const [targetWins, setTargetWins] = useState(2);
    const [newGame, setNewGame] = useState<NewGameResponse | null>(null);
    const [gameCode, setGameCode] = useState("");
    const [hubState, setHubState] = useState<string>("Disconnected");
    const [gameState, setGameState] = useState<GameState | null>(null);
    const [logs, setLogs] = useState<string[]>([]);

    const hub = useMemo(() => createGameHub(), []);

    function log(line: string) {
        setLogs((prev) => [`${new Date().toLocaleTimeString()} • ${line}`, ...prev].slice(0, 200));
    }

    useEffect(() => {
        hub.onreconnecting(() => setHubState("Reconnecting"));
        hub.onreconnected(() => setHubState("Connected"));
        hub.onclose(() => setHubState("Disconnected"));

        hub.on("ConnectedToGame", (payload: JoinGameResponse) => {
            setGameState(payload.state);
            log("Connected to game");
        });
        hub.on("PlayersUpdated", (players) => {
            setGameState((s) => (s ? { ...s, players } : s));
            log("Players updated");
        });
        hub.on("MatchStarted", (state: GameState) => { setGameState(state); log("Match started"); });
        hub.on("RoundEnded", (state: GameState) => { setGameState(state); log("Round ended"); });
        hub.on("NewRoundStarted", (state: GameState) => { setGameState(state); log("New round started"); });
        hub.on("GameUpdated", (state: GameState) => setGameState(state));

        return () => { hub.stop().catch(() => { }); };
    }, [hub]);

    async function createGame() {
        try {
            const res = await Api.createGame({ playerCount, targetWins });
            setNewGame(res);
            onToast(`Game created: ${res.gameId}`);
        } catch (e: any) { onToast(`Create game failed: ${e.message || e}`); }
    }

    async function joinGame() {
        try {
            let uid = getCookie();
            if (!uid) uid = await onRequireTempUser();
            if (hub.state !== "Connected") await hub.start();
            setHubState(hub.state);
            await hub.invoke("ConnectToGame", gameCode.trim(), uid, null);
            onToast("Join requested…");
        } catch (e: any) { onToast(`Join failed: ${e.message || e}`); }
    }

    return (
        <Card title="Games">
            <div className="grid grid-cols-2 gap-3">
                <div>
                    <label className="text-sm text-white/70">Players</label>
                    <input className="w-full rounded-xl border border-white/10 bg-zinc-800 text-white px-3 py-2" type="number" min={2} max={8} value={playerCount} onChange={(e) => setPlayerCount(parseInt(e.target.value || "2"))} />
                </div>
                <div>
                    <label className="text-sm text-white/70">Target Wins</label>
                    <input className="w-full rounded-xl border border-white/10 bg-zinc-800 text-white px-3 py-2" type="number" min={1} max={10} value={targetWins} onChange={(e) => setTargetWins(parseInt(e.target.value || "2"))} />
                </div>
                <div className="col-span-2"><Button onClick={createGame}>Create Game</Button></div>
            </div>

            {newGame && (
                <div className="mt-3 text-xs text-white/70 break-all">
                    <div>GameId: <span className="text-white">{newGame.gameId}</span></div>
                    <div>NextSeat: {newGame.nextSeat}</div>
                    <div>Players: {newGame.playerCount}</div>
                    <div>TargetWins: {newGame.targetWins}</div>
                </div>
            )}

            <div className="mt-4 grid grid-cols-[1fr_auto] gap-2 items-end">
                <div>
                    <label className="text-sm text-white/70">Game Code</label>
                    <input className="w-full rounded-xl border border-white/10 bg-zinc-800 text-white px-3 py-2" placeholder="e.g. ABCD" value={gameCode} onChange={(e) => setGameCode(e.target.value)} />
                </div>
                <Button onClick={joinGame}>Join</Button>
            </div>

            <div className="mt-4 text-xs text-white/60">Hub: {hubState}</div>

            <h3 className="text-white/80 font-medium text-sm mt-4">Live State</h3>
            <div className="bg-black/40 rounded-xl p-3 border border-white/10 text-xs overflow-auto max-h-64">
                {gameState ? <pre className="whitespace-pre-wrap">{JSON.stringify(gameState, null, 2)}</pre> : <div className="text-white/50">Not connected.</div>}
            </div>

            <h3 className="text-white/80 font-medium text-sm mt-4">Events</h3>
            <div className="bg-black/40 rounded-xl p-3 border border-white/10 text-xs overflow-auto max-h-40 whitespace-pre-wrap">
                {logs.length ? logs.join("\n") : <span className="text - white / 50">No events yet.</span>}
      </div>
        </Card>
    );
}
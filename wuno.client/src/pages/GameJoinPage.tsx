import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getCookie } from "@/auth/cookies";
import { Api } from "@/api/client";
import { useUser } from "@/context/UserContext";
import { useToast } from "@/context/ToastContext";
import { setPendingJoin, clearPendingJoin } from "@/utils/pendingJoin";
import { createGameHub } from "@/hub/connection";
import type { HubConnection } from "@microsoft/signalr";
import type { JoinGameResponse } from "@/api/types";

export default function GameJoinPage() {
    const { code = "" } = useParams();
    const nav = useNavigate();
    const { user, setUser } = useUser();
    const { push } = useToast();
    const [msg, setMsg] = useState("Preparing to join…");
    const hubRef = useRef<HubConnection | null>(null);
    const ran = useRef(false);

    useEffect(() => {
        if (ran.current) return;
        ran.current = true;

        (async () => {
            const gameCode = code.trim().toUpperCase();
            if (!gameCode) {
                push("Invalid game link.");
                nav("/lobby", { replace: true });
                return;
            }

            // If no cookie, remember intent and go to landing
            const uid = getCookie();
            if (!uid) {
                setPendingJoin(gameCode);
                nav("/", { replace: true });
                return;
            }

            // Ensure we have a valid user in context
            try {
                if (!user) {
                    const u = await Api.getUser(uid);
                    if (!u?.ok) throw new Error("Invalid session.");
                    setUser(u);
                }
            } catch {
                setPendingJoin(gameCode);
                nav("/", { replace: true });
                return;
            }

            // Connect to SignalR and join
            try {
                setMsg("Connecting to game…");
                const hub = createGameHub();
                hubRef.current = hub;
                await hub.start();

                // Navigate to real game page once hub confirms join (when you add it)
                hub.on("ConnectedToGame", (res: JoinGameResponse) => {
                    clearPendingJoin();
                    setMsg("Joined! Redirecting…");
                    // When you build the in-game UI, push to that route:
                    // nav(`/game/${res.state.gameId}`, { replace: true });
                    // For now, return to lobby after successful join:
                    nav("/lobby", { replace: true });
                });

                await hub.invoke("ConnectToGame", gameCode, uid, null);
            } catch (e: any) {
                console.error(e);
                push("Could not join that game. It may be full, closed, or the code is wrong.");
                clearPendingJoin();
                nav("/lobby", { replace: true });
            }
        })();

        return () => {
            hubRef.current?.stop().catch(() => { });
        };
    }, [code, nav, push, setUser, user]);

    return (
        <div className="container mx-auto px-4 py-16">
            <div className="card max-w-xl mx-auto shadow">
                <div className="card-body">
                    <h5 className="card-title">Joining Game</h5>
                    <p className="opacity-80">{msg}</p>
                </div>
            </div>
        </div>
    );
}

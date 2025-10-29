import React, { useState } from "react";
import AccountPanel from "@/features/account/AccountPanel";
import GamePanel from "@/features/games/GamePanel";
import { Api } from "@/api/client";
import { TmpUserRequest, UserResponse } from "@/api/types";
import { setCookie } from "@/auth/cookies";

export default function App() {
    const [toast, setToast] = useState<string | null>(null);

    async function requireTempUser(): Promise<string> {
        const name = window.prompt("Enter a display name for your temp account:", "Guest") || "Guest";
        const body: TmpUserRequest = { name, iconUrl: null, email: null };
        const res: UserResponse = await Api.createTempUser(body);
        if (!res.userId) throw new Error("No userId returned");
        setCookie(res.userId);
        return res.userId;
    }

    return (
        <div className="min-h-dvh bg-gradient-to-b from-zinc-950 to-zinc-900 text-white">
            <div className="max-w-5xl mx-auto px-4 py-8">
                <header className="mb-6">
                    <h1 className="text-2xl font-bold">Wuno – Client</h1>
                    <p className="text-white/60 text-sm">Landing page · temp token · account · create & join games</p>
                </header>

                <div className="grid md:grid-cols-2 gap-5">
                    <AccountPanel onToast={(s) => setToast(s)} />
                    <GamePanel onToast={(s) => setToast(s)} onRequireTempUser={requireTempUser} />
                </div>

                <div className="mt-6 text-xs text-white/50">{toast}</div>
            </div>
        </div>
    );
}
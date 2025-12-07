import { Api } from "@/api/client";
import type { User } from "@/context/UserContext";

export async function tryAutoRejoin(user: User | null): Promise<string | null> {
    if (!user) return null;
    const r = await Api.activeForCurrent();
    return r?.ok && r.inGame && r.gameCode ? r.gameCode : null;
}

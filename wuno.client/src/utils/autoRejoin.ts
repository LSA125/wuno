import { Api } from "@/api/client";
import type { User } from "@/context/UserContext";

export async function tryAutoRejoin(user: User | null): Promise<string | null> {
    if (!user) return null;
    const r = user.registered ? await Api.meActiveGame() : await Api.guestActiveGame(user.userId);
    return r?.ok && r.inGame && r.gameCode ? r.gameCode : null;
}

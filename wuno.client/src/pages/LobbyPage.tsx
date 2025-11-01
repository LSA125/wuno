import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import CreateOrJoin from "@/components/lobby/CreateOrJoin";
import { getCookie } from "@/auth/cookies";
import { useUser } from "@/context/UserContext";
import { Api, Auth } from "@/api/client";
import { clearPendingJoin } from "@/utils/pendingJoin";

export default function LobbyPage() {
    const { setUser } = useUser();
    const nav = useNavigate();
    const [loaded, setLoaded] = useState(false);

    useEffect(() => { clearPendingJoin(); }, []);

    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                // 1) Try registered session first (HttpOnly auth cookie)
                const me = await Auth.me();
                if (!cancelled && me?.ok) {
                    setUser(me);
                    setLoaded(true);
                    return;
                }
            } catch { /* fall through to guest */ }

            try {
                // 2) Try guest cookie fallback
                const uid = getCookie();
                if (!uid) { if (!cancelled) nav("/", { replace: true }); return; }
                const u = await Api.getUser(uid);
                if (!cancelled && u?.ok) {
                    setUser(u);
                    setLoaded(true);
                    return;
                }
            } catch { /* ignore */ }

            if (!cancelled) nav("/", { replace: true });
        })();
        return () => { cancelled = true; };
    }, [nav, setUser]);

    if (!loaded) {
        return (
            <div className="container mx-auto px-4 my-10">
                <div className="card max-w-2xl mx-auto shadow">
                    <div className="card-body">Loading…</div>
                </div>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 my-10">
            <section className="py-10 relative">
                <CreateOrJoin />
            </section>
        </div>
    );
}

// src/components/Lobby/UserGate.tsx
import { useEffect, useRef } from "react";
import { getCookie } from "@/auth/cookies";
import { Api } from "@/api/client";
import { useUser } from "@/context/UserContext";
import { useNavigate, useLocation } from "react-router-dom";

export default function UserGate() {
    const { setUser } = useUser();
    const nav = useNavigate();
    const loc = useLocation();
    const ran = useRef(false);

    useEffect(() => {
        if (ran.current) return;
        ran.current = true;

        (async () => {
            const uid = getCookie();
            if (!uid) {
                if (loc.pathname !== "/") nav("/", { replace: true });
                return;
            }
            try {
                const profile = await Api.getUser(uid);
                if (!profile?.ok) {
                    if (loc.pathname !== "/") nav("/", { replace: true });
                    return;
                }
                setUser(profile); // ok: no nav here
            } catch {
                if (loc.pathname !== "/") nav("/", { replace: true });
            }
        })();
    }, []);

    return null;
}

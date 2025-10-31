// src/pages/LobbyPage.tsx
import UserGate from "@/components/lobby/UserGate";
import CreateOrJoin from "@/components/lobby/CreateOrJoin";
import { clearCookie } from "@/auth/cookies";
import { useUser } from "@/context/UserContext";
import { useToast } from "@/context/ToastContext";
import { useNavigate } from "react-router-dom";
import { clearPendingJoin } from "@/utils/pendingJoin";
import { useEffect } from "react";

export default function LobbyPage() {
    useEffect(() => { clearPendingJoin(); }, []);
    return (
        <div className="container mx-auto px-4 my-10">
            <UserGate />
            <section className="py-10 relative">
                <CreateOrJoin />
            </section>

        </div>
    );
}

import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import CreateOrJoin from "@/components/lobby/CreateOrJoin";
import { useUser, normalizeUser } from "@/context/UserContext";
import { clearPendingJoin } from "@/utils/pendingJoin";
import { useToast } from "@/context/ToastContext";
import { tryAutoRejoin } from "@/utils/autoRejoin";
export default function LobbyPage() {
    const { user, setUser } = useUser();
    const nav = useNavigate();
    const [loaded, setLoaded] = useState(false);
    const { push } = useToast();
    useEffect(() => { clearPendingJoin(); }, []);

    useEffect(() => {
        if (!user) {
            nav("/", { replace: true });
            return;
        }

        (async () => {
            const auto = await tryAutoRejoin(user);
            if (auto) {
                nav(`/game/${auto}`, { replace: true });
            } else {
                setLoaded(true);
            }
        })();
    }, [user, nav]);

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

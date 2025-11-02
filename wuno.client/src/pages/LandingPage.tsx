import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getCookie, setCookie } from "@/auth/cookies";
import { Api, Auth } from "@/api/client";
import { normalizeUser, useUser } from "@/context/UserContext";
import FirstTimeModal from "@/components/lobby/FirstTimeModal";
import RegisterModal from "@/components/lobby/RegisterModal";
import { getPendingJoin } from "@/utils/pendingJoin";
import SignInModal from "@/components/auth/SignInModal";
import { useToast } from "@/context/ToastContext";
import type { UserResponse } from "@/api/types";
import { tryAutoRejoin } from "@/utils/autoRejoin";

export default function LandingPage() {
    const nav = useNavigate();
    const { setUser } = useUser();
    const [showSignIn, setShowSignIn] = useState(false);
    const [showGuest, setShowGuest] = useState(false);
    const [showRegister, setShowRegister] = useState(false);
    const [loading, setLoading] = useState(true);
    const ran = useRef(false);

    const { push } = useToast();
    useEffect(() => {
        if (sessionStorage.getItem("auth_expired") === "1") {
            push("Your session expired. Please sign in again.");
            sessionStorage.removeItem("auth_expired");
        }
        if (sessionStorage.getItem("signed_out") === "1") {
            push("You’ve been signed out successfully.");
            sessionStorage.removeItem("signed_out");
        }
    }, [push]);

    useEffect(() => {
        if (ran.current) return;
        ran.current = true;

        (async () => {
            // 1) Registered session
            try {
                const me: UserResponse = await Auth.meSafe();
                if (me?.ok) {
                    const u = normalizeUser(me, true);
                    setUser(u);

                    const auto = await tryAutoRejoin(u);
                    const pending = getPendingJoin();
                    nav((pending || auto) ? `/game/${pending || auto}` : "/lobby", { replace: true });
                    return;
                }
            } catch { }

            // 2) Guest cookie
            const uid = getCookie();
            if (uid) {
                try {
                    const res: UserResponse = await Api.getUser(uid);
                    if (res?.ok) {
                        const u = normalizeUser(res, false);
                        setUser(u);

                        const auto = await tryAutoRejoin(u);
                        const pending = getPendingJoin();
                        nav((pending || auto) ? `/game/${pending || auto}` : "/lobby", { replace: true });
                        return;
                    }
                } catch { }
            }

            setLoading(false);
        })();
    }, [nav, setUser]);

    const afterAuth = async (id: string, ures: UserResponse) => {
        setCookie(id);
        const u = normalizeUser(ures, false);
        setUser(u);
        const auto = await tryAutoRejoin(u);
        const pending = getPendingJoin();
        nav((pending || auto) ? `/game/${pending || auto}` : "/lobby", { replace: true });
    };

    const onSignedIn = async () => {
        const me: UserResponse = await Auth.me();
        if (me?.ok) {
            const u = normalizeUser(me, true);
            setUser(u);
            const auto = await tryAutoRejoin(u);
            const pending = getPendingJoin();
            nav((pending || auto) ? `/game/${pending || auto}` : "/lobby", { replace: true });
        }
    };

    if (loading) return null;

    return (
        <div className="container mx-auto px-4 min-h-[calc(100vh-64px)] flex items-center justify-center">
            <div className="w-full max-w-3xl">
                <div className="card shadow-2xl">
                    <div className="text-center mb-8">
                        <h1 className="display-4">Wuno</h1>
                        <p className="lead opacity-80">Fast party word game. Choose how you want to jump in.</p>
                    </div>
                    <div className="card-body py-10">
                        <div className="flex flex-col md:flex-row items-center justify-center gap-4">
                            <button className="btn btn-primary btn-lg" onClick={() => setShowGuest(true)}>
                                Play as Guest
                            </button>
                            <span className="opacity-60">or</span>
                            <button className="btn btn-success btn-lg" onClick={() => setShowRegister(true)}>
                                Create Account
                            </button>
                            <span className="opacity-60">or</span>
                            <button className="btn btn-outline-dark btn-lg" onClick={() => setShowSignIn(true)}>
                                Sign In
                            </button>
                        </div>
                        <p className="text-center mt-4 opacity-70">
                            You can register later from your profile, too.
                        </p>
                    </div>
                </div>
            </div>

            {showGuest && (
                <FirstTimeModal open onClose={() => setShowGuest(false)} onSuccess={(id, u) => afterAuth(id, u)} />
            )}
            {showRegister && <RegisterModal open onClose={() => setShowRegister(false)} />}
            {showSignIn && <SignInModal open onClose={() => setShowSignIn(false)} onSuccess={onSignedIn} />}
        </div>
    );
}

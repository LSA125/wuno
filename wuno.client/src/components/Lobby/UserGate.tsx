import { useEffect, useState } from "react";
import { getCookie, userCookieKey, setCookie } from "@/auth/cookies";
import { Api } from "@/api/client";
import { useUser } from "@/context/UserContext";
import FirstTimeModal from "./FirstTimeModal";

export default function UserGate() {
    const { user, setUser } = useUser();
    const [showFirstTime, setShowFirstTime] = useState(false);

    useEffect(() => {
        const uid = getCookie();
        (async () => {
            if (uid) {
                try {
                    const profile = await Api.getUser(uid);
                    setUser(profile);
                    if (!profile?.ok) setShowFirstTime(true);
                } catch {
                    setShowFirstTime(true);
                }
            } else {
                setShowFirstTime(true);
            }
        })();
    }, [setUser]);

    // When a new anon is created, store cookie and user in context
    const onFirstTimeSuccess = (newId: string, u: any) => {
        setCookie(newId, 365);
        setUser(u);
        setShowFirstTime(false);
    };

    return (
        <>
            {showFirstTime && (
                <FirstTimeModal open onClose={() => { }} onSuccess={onFirstTimeSuccess} />
            )}
        </>
    );
}

import React, { useEffect, useState } from "react";
import Button from "@/components/ui/Button";
import Card from "@/components/ui/Card";
import Field from "@/components/ui/Field";
import { Api } from "@/api/client";
import { RegUserRequest, TmpUserRequest, UserResponse } from "@/api/types";
import { clearCookie, getCookie, setCookie, userCookieKey } from "@/auth/cookies";


export default function AccountPanel({ onToast }: { onToast: (s: string) => void }) {
    const [userId, setUserId] = useState<string | null>(null);
    const [user, setUser] = useState<UserResponse | null>(null);
    const [isRegistered, setIsRegistered] = useState(false);
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [iconUrl, setIconUrl] = useState("");
    const [password, setPassword] = useState("");


    useEffect(() => {
        const existing = getCookie();
        if (existing) {
            setUserId(existing);
            loadUser(existing);
        }
    }, []);


    async function loadUser(id: string) {
        try {
            const u = await Api.getUser(id);
            setUser(u);
            const reg = !!u?.name && !!u?.email && !!u?.userId && !u?.msg;
            setIsRegistered(reg);
            setName(u.name || "");
            setEmail(u.email || "");
            setIconUrl(u.iconUrl || "");
        } catch (e: any) {
            onToast(`Failed to load user: ${e.message || e}`);
        }
    }


    async function ensureTempUser(promptIfMissing = true) {
        if (userId) return userId;
        let tempName = name.trim();
        if (!tempName && promptIfMissing) tempName = window.prompt("Enter a display name:", "Guest") || "Guest";
        const body: TmpUserRequest = { name: tempName || "Guest", iconUrl: iconUrl || null, email: email || null };
        const res = await Api.createTempUser(body);
        if (!res.userId) throw new Error("No userId returned");
        setCookie(res.userId);
        setUserId(res.userId);
        setUser(res);
        setIsRegistered(false);
        setName(res.name || tempName || "");
        setEmail(res.email || "");
        setIconUrl(res.iconUrl || "");
        onToast("Temporary token created.");
        return res.userId;
    }
}
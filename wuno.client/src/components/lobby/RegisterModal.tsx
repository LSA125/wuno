// src/components/Lobby/RegisterModal.tsx
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Auth } from "@/api/client";
import { normalizeUser, useUser } from "@/context/UserContext";
import { getCookie, clearCookie } from "@/auth/cookies";
import { getPendingJoin } from "@/utils/pendingJoin";
import { UserResponse } from "@/api/types";

export default function RegisterModal({ open, onClose }: { open: boolean; onClose: () => void; }) {
    const { setUser } = useUser();
    const nav = useNavigate();
    const [username, setUsername] = useState("");
    const [pass, setPass] = useState("");
    const [email, setEmail] = useState("");
    const [iconUrl, setIconUrl] = useState("");
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState<string | null>(null);

    const submit = async () => {
        setErr(null); setBusy(true);
        try {
            const tempId = getCookie();
            await Auth.register({
                tempUserId: tempId || undefined,
                username,
                password: pass,
                email: email || null,
                iconUrl: iconUrl || null,
            });

            const me: UserResponse = await Auth.me();
            if (!me?.ok) throw new Error("Could not load profile after registration.");
            setUser(normalizeUser(me, true));

            // Clear temp cookie if it existed
            if (tempId) clearCookie();

            const pending = getPendingJoin?.();
            if (pending) nav(`/game/${pending}`, { replace: true });
            else nav("/lobby", { replace: true });

            onClose();
        } catch (e: any) {
            const err = e as any;
            setErr(
                err?.message
                ?? err?.msg
                ?? err?.error
                ?? "Registration failed."
            );
        } finally {
            setBusy(false);
            setPass(""); // don’t keep password in memory
        }
    };

    if (!open) return null;
    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4">
            <div className="card w-full max-w-lg">
                <div className="card-header"><h5 className="card-title m-0">Create Account</h5></div>
                <div className="card-body">
                    <div className="mb-3">
                        <label className="form-label">Username</label>
                        <input className="form-control" value={username} onChange={e => setUsername(e.target.value)} autoComplete="username" />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Password</label>
                        <input className="form-control" type="password" value={pass} onChange={e => setPass(e.target.value)} autoComplete="new-password" />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Email (optional)</label>
                        <input className="form-control" type="email" value={email} onChange={e => setEmail(e.target.value)} autoComplete="email" />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Icon URL (optional)</label>
                        <input className="form-control" value={iconUrl} onChange={e => setIconUrl(e.target.value)} />
                    </div>
                    {err && <div className="alert alert-danger py-2">{err}</div>}
                </div>
                <div className="card-footer flex justify-end gap-2">
                    <button className="btn btn-secondary" onClick={onClose} disabled={busy}>Cancel</button>
                    <button className="btn btn-primary" onClick={submit} disabled={busy}>
                        {busy ? "Creating…" : "Create Account"}
                    </button>
                </div>
            </div>
        </div>
    );
}

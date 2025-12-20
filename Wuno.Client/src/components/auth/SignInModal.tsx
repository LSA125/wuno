import { useState } from "react";
import { Auth } from "@/api/client";
import type { AuthResponse } from "@/api/types";

export default function SignInModal({
    open, onClose, onSuccess
}: { open: boolean; onClose: () => void; onSuccess: (res: AuthResponse) => void; }) {
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState<string | null>(null);

    const submit = async () => {
        setErr(null); setBusy(true);
        try {
            const res = await Auth.login({ username, password });
            setPassword(""); // don’t keep it in memory any longer than needed
            onSuccess(res);
        } catch (e: any) {
            setErr(e?.message || e?.msg || "Sign in failed.");
        } finally {
            setBusy(false);
        }
    };

    if (!open) return null;
    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4">
            <div className="card w-full max-w-md">
                <div className="card-header"><h5 className="card-title m-0">Sign In</h5></div>
                <div className="card-body">
                    <div className="mb-3">
                        <label className="form-label">Username</label>
                        <input className="form-control" value={username} onChange={e => setUsername(e.target.value)} autoComplete="username" />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Password</label>
                        <input className="form-control" type="password" value={password} onChange={e => setPassword(e.target.value)} autoComplete="current-password" />
                    </div>
                    {err && <div className="alert alert-danger py-2">{err}</div>}
                </div>
                <div className="card-footer flex justify-end gap-2">
                    <button className="btn btn-secondary" onClick={onClose} disabled={busy}>Cancel</button>
                    <button className="btn btn-primary" onClick={submit} disabled={busy}>
                        {busy ? "Signing in..." : "Sign In"}
                    </button>
                </div>
            </div>
        </div>
    );
}

import { useState } from "react";
import { Api } from "@/api/client";
import type { TmpUserRequest, UserResponse } from "@/api/types";

export default function FirstTimeModal({
    open,
    onClose,
    onSuccess,
}: {
    open: boolean;
    onClose: () => void;
    onSuccess: (newId: string, u: UserResponse) => void;
}) {
    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [iconUrl, setIconUrl] = useState("");
    const [err, setErr] = useState<string | null>(null);
    const [busy, setBusy] = useState(false);

    const submit = async () => {
        setErr(null);
        if (!name.trim()) { setErr("Please enter a username."); return; }
        setBusy(true);
        try {
            const body: TmpUserRequest = { name: name.trim(), email: email || null, iconUrl: iconUrl || null };
            const res = await Api.createTempUser(body);
            if (res.ok && res.userId) onSuccess(res.userId, res);
            else setErr(res.msg || "Failed to create user.");
        } catch (e: any) {
            setErr(e.message || "Failed to create user.");
        } finally {
            setBusy(false);
        }
    };

    if (!open) return null;
    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4">
            <div className="card w-full max-w-lg shadow-2xl">
                <div className="card-header">
                    <h5 className="card-title m-0">Welcome! Create a temporary profile</h5>
                </div>
                <div className="card-body">
                    <div className="mb-3">
                        <label className="form-label">Username *</label>
                        <input className="form-control" value={name} onChange={e => setName(e.target.value)} />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Email (optional)</label>
                        <input className="form-control" type="email" value={email} onChange={e => setEmail(e.target.value)} />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Icon URL (optional)</label>
                        <input className="form-control" value={iconUrl} onChange={e => setIconUrl(e.target.value)} />
                    </div>
                    {err && <div className="alert alert-danger py-2">{err}</div>}
                </div>
                <div className="card-footer flex gap-2 justify-end">
                    <button className="btn btn-secondary" onClick={onClose} disabled={busy}>Cancel</button>
                    <button className="btn btn-primary" onClick={submit} disabled={busy}>
                        {busy ? "Creating..." : "Create"}
                    </button>
                </div>
            </div>
        </div>
    );
}

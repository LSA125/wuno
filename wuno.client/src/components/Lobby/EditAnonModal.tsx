import { useState } from "react";
import { Api } from "@/api/client";
import { useUser } from "@/context/UserContext";
import type { TmpUserRequest } from "@/api/types";

export default function EditAnonModal({ open, onClose }: { open: boolean; onClose: () => void; }) {
    const { user, setUser } = useUser();
    const [name, setName] = useState(user?.name ?? "");
    const [email, setEmail] = useState(user?.email ?? "");
    const [iconUrl, setIconUrl] = useState(user?.iconUrl ?? "");
    const [busy, setBusy] = useState(false);
    const [err, setErr] = useState<string | null>(null);

    const submit = async () => {
        if (!user?.userId) return;
        setBusy(true); setErr(null);
        try {
            const body: TmpUserRequest = {
                userId: user.userId,
                name: name || "",
                email: email || null,
                iconUrl: iconUrl || null,
            };
            const res = await Api.editAnon(user.userId, body);
            if (res.ok) setUser(res);
            else setErr(res.msg || "Failed to edit temporary account.");
            onClose();
        } catch (e: any) {
            setErr(e.message || "Failed to edit temporary account.");
        } finally {
            setBusy(false);
        }
    };

    if (!open) return null;
    return (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4">
            <div className="card w-full max-w-lg">
                <div className="card-header"><h5 className="card-title m-0">Edit Temporary Account</h5></div>
                <div className="card-body">
                    <div className="mb-3">
                        <label className="form-label">Username</label>
                        <input className="form-control" value={name} onChange={e => setName(e.target.value)} />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Email (optional)</label>
                        <input className="form-control" value={email} onChange={e => setEmail(e.target.value)} />
                    </div>
                    <div className="mb-3">
                        <label className="form-label">Icon URL (optional)</label>
                        <input className="form-control" value={iconUrl} onChange={e => setIconUrl(e.target.value)} />
                    </div>
                    {err && <div className="alert alert-danger py-2">{err}</div>}
                </div>
                <div className="card-footer flex justify-end gap-2">
                    <button className="btn btn-secondary" onClick={onClose} disabled={busy}>Cancel</button>
                    <button className="btn btn-primary" onClick={submit} disabled={busy}>{busy ? "Saving..." : "Save"}</button>
                </div>
            </div>
        </div>
    );
}

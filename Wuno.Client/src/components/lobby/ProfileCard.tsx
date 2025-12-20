import { useUser } from "@/context/UserContext";
import EditAnonModal from "./EditAnonModal";
import RegisterModal from "./RegisterModal";
import EditRegisteredModal from "./EditRegisteredModal";
import { useState } from "react";
import { clearCookie } from "@/auth/cookies";
import { useToast } from "@/context/ToastContext";
import { useNavigate } from "react-router-dom";
import { Auth, clearAccessToken } from "@/api/client";
import DefaultAvatar from "@/assets/DefaultAvatar.svg"
export default function ProfileCard() {
    const { user, setUser } = useUser();
    const [showEditAnon, setShowEditAnon] = useState(false);
    const [showRegister, setShowRegister] = useState(false);
    const [showEditReg, setShowEditReg] = useState(false);

    const { push } = useToast();
    const nav = useNavigate();
    const handleSignOut = async () => {
        try {
            if (user?.registered) {
                // ensure this request survives navigation
                await Auth.logout();
            }
        } catch (e) {
            push("Error signing out");
        } finally {
            // local cleanup always
            setUser(null);
            clearCookie();                           // clears *guest* cookie only
            clearAccessToken();                      // clears access token from localStorage
            sessionStorage.setItem("signed_out", "1");

            // if you have a SignalR connection, stop it here to avoid auto-rejoins

            nav("/", { replace: true })
        }
    };
    return (
        <div className="card shadow-lg">
            <div className="card-body">
                <button
                    onClick={handleSignOut}
                    className="btn btn-sm btn-outline-secondary position-absolute top-0 end-0 m-1"
                >
                    Sign Out
                </button>
                <h5 className="card-title">Your Profile</h5>
                {user?.iconUrl && (
                    <div className="mb-3">
                        <img src={user.iconUrl || DefaultAvatar} alt="icon" className="rounded-circle border" width={72} height={72} />
                    </div>
                )}
                <dl className="mb-4">
                    <dt className="fw-bold">User ID</dt>
                    <dd className="text-break">{user?.userId || "—"}</dd>
                    <dt className="fw-bold">Name</dt>
                    <dd>{user?.name || "—"}</dd>
                    <dt className="fw-bold">Email</dt>
                    <dd>{user?.email || "—"}</dd>
                </dl>

                {!user?.registered ? (
                    <div className="flex flex-col gap-2">
                        <button className="btn btn-outline-primary" onClick={() => setShowEditAnon(true)}>
                            Edit Temporary Account
                        </button>
                        <button className="btn btn-primary" onClick={() => setShowRegister(true)}>
                            Register Account
                        </button>
                    </div>
                ) : (
                    <div className="flex flex-col gap-2">
                        <button className="btn btn-primary" onClick={() => setShowEditReg(true)}>
                            Edit Registered Account
                        </button>
                    </div>
                )}
            </div>

            <EditAnonModal open={showEditAnon} onClose={() => setShowEditAnon(false)} />
            <RegisterModal open={showRegister} onClose={() => setShowRegister(false)} />
            <EditRegisteredModal open={showEditReg} onClose={() => setShowEditReg(false)} />
        </div>
    );
}

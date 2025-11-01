import { createContext, useContext, useRef, useState, useCallback, ReactNode, useEffect } from "react";
import { createPortal } from "react-dom";

type Toast = { id: number; msg: string };
type ToastCtx = {
    toasts: Toast[];
    push: (msg: string) => void;
    remove: (id: number) => void;
};

const ToastContext = createContext<ToastCtx | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<Toast[]>([]);
    const timeouts = useRef(new Map<number, number>());

    const remove = useCallback((id: number) => {
        setToasts((t) => t.filter((x) => x.id !== id));
        const tid = timeouts.current.get(id);
        if (tid) {
            window.clearTimeout(tid);
            timeouts.current.delete(id);
        }
    }, []);

    const push = useCallback((msg: string) => {
        const id = Date.now() + Math.floor(Math.random() * 1000);
        setToasts((t) => [...t, { id, msg }]);
        const tid = window.setTimeout(() => remove(id), 4000);
        timeouts.current.set(id, tid);
    }, [remove]);

    useEffect(() => {
        return () => {
            timeouts.current.forEach((tid) => window.clearTimeout(tid));
            timeouts.current.clear();
        };
    }, []);

    return (
        <ToastContext.Provider value={{ toasts, push, remove }}>
            {children}
            {createPortal(
                <div
                    // Use inline zIndex so Tailwind purge/config can’t strip it.
                    style={{ position: "fixed", top: "0.75rem", right: "0.75rem", zIndex: 2147483647, pointerEvents: "none" }}
                    aria-live="polite"
                    aria-atomic="true"
                >
                    {toasts.map((t) => (
                        <div
                            key={t.id}
                            role="status"
                            style={{ pointerEvents: "auto" }}
                            className="shadow-md rounded-md px-4 py-3 text-sm font-medium border bg-white text-black toast-enter"
                        >
                            {t.msg}
                        </div>
                    ))}
                </div>,
                document.body
            )}
        </ToastContext.Provider>
    );
}

export function useToast() {
    const ctx = useContext(ToastContext);
    if (!ctx) throw new Error("useToast must be used inside ToastProvider");
    return ctx;
}

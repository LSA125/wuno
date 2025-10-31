import { createContext, useContext, useState, ReactNode } from "react";

type Toast = { id: number; msg: string };
type ToastCtx = {
    toasts: Toast[];
    push: (msg: string) => void;
    remove: (id: number) => void;
};

const ToastContext = createContext<ToastCtx | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
    const [toasts, setToasts] = useState<Toast[]>([]);

    const push = (msg: string) => {
        const id = Date.now();
        setToasts((t) => [...t, { id, msg }]);
        setTimeout(() => remove(id), 4000);
    };

    const remove = (id: number) => {
        setToasts((t) => t.filter((x) => x.id !== id));
    };

    return (
        <ToastContext.Provider value={{ toasts, push, remove }}>
            {children}
            <div className="fixed top-3 right-3 space-y-2 z-50">
                {toasts.map((t) => (
                    <div
                        key={t.id}
                        className="alert alert-info shadow-md border border-gray-300 bg-white/90 backdrop-blur-sm"
                    >
                        {t.msg}
                    </div>
                ))}
            </div>
        </ToastContext.Provider>
    );
}

export function useToast() {
    const ctx = useContext(ToastContext);
    if (!ctx) throw new Error("useToast must be used inside ToastProvider");
    return ctx;
}

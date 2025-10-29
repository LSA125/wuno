import React from "react";


export default function Button({ variant = "primary", className = "", ...props }: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "ghost" | "danger" }) {
    const styles =
        variant === "primary"
            ? "bg-emerald-500 hover:bg-emerald-400 text-emerald-950"
            : variant === "danger"
                ? "bg-rose-500 hover:bg-rose-400 text-rose-950"
                : "border border-white/15 text-white/90 hover:bg-white/5";
    return <button {...props} className={`px-3 py-2 rounded-xl font-medium transition ${styles} ${className}`} />;
}
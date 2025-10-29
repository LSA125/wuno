import React from "react";


export default function Field({ label, children }: { label: string; children: React.ReactNode }) {
    return (
        <label className="grid grid-cols-[140px_1fr] gap-3 items-center py-1">
            <span className="text-sm text-white/70">{label}</span>
            {children}
        </label>
    );
}
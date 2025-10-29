import React from "react";


export default function Card({ title, children }: { title: string; children: React.ReactNode }) {
    return (
        <div className="bg-zinc-900/60 border border-white/10 rounded-2xl p-5 shadow-lg">
            <div className="flex items-center justify-between mb-3">
                <h2 className="text-lg font-semibold text-white/90">{title}</h2>
            </div>
            {children}
        </div>
    );
}
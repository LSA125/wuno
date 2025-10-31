import { ReactNode } from "react";

export default function Shell({ children }: { children: ReactNode }) {
    return (
        <div className="min-h-screen flex flex-col">
            {/* no header */}
            <main className="flex-1">{children}</main>
            <footer className="py-6 text-center text-sm opacity-70">
                &copy; {new Date().getFullYear()} Wuno
            </footer>
        </div>
    );
}

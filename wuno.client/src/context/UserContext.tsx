import { createContext, useContext, useState, useMemo, ReactNode } from "react";
import type { UserResponse } from "@/api/types";

type User = {
    ok: boolean;
    userId?: string | null;
    name?: string | null;
    iconUrl?: string | null;
    email?: string | null;
    msg?: string | null;
} | null;

type Ctx = {
    user: User;
    setUser: (u: User) => void;
};

const UserContext = createContext<Ctx | null>(null);

export function UserProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User>(null);
    const value = useMemo(() => ({ user, setUser }), [user]);
    return <UserContext.Provider value={value}>{children}</UserContext.Provider>;
}

export function useUser() {
    const ctx = useContext(UserContext);
    if (!ctx) throw new Error("useUser must be used within UserProvider");
    return ctx;
}

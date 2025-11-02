import { createContext, useContext, useState, useMemo, ReactNode } from "react";
import { UserResponse } from "@/api/types";

export type User = {
    userId: string;                    // always required
    name: string;                  // always required
    iconUrl?: string | null;
    email?: string | null;
    registered: boolean;           // true if authenticated via ASP.NET Core
};

type Ctx = {
    user: User | null;
    setUser: (u: User | null) => void;
};

const UserContext = createContext<Ctx | null>(null);

export function UserProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const value = useMemo(() => ({ user, setUser }), [user]);
    return <UserContext.Provider value={value}>{children}</UserContext.Provider>;
}

export function useUser() {
    const ctx = useContext(UserContext);
    if (!ctx) throw new Error("useUser must be used within UserProvider");
    return ctx;
}


export function normalizeUser(res: UserResponse, isRegistered: boolean): User {
    return {
        userId: res.userId!,
        name: res.name || "Guest",
        iconUrl: res.iconUrl,
        email: res.email,
        registered: isRegistered,
    };
}

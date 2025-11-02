// Lightweight JSON client + typed helpers for your endpoints
import { RegUserRequest, TmpUserRequest, UserResponse, NewGameRequest, NewGameResponse, GameCodeResponse } from "./types";


export async function request<T>(
    input: RequestInfo,
    init?: RequestInit,
    opts?: { ignore401?: boolean }     // <-- add this
): Promise<T> {
    const res = await fetch(input, {
        credentials: "include",
        headers: {
            "Content-Type": "application/json",
            "X-Requested-With": "XMLHttpRequest",
            ...(init?.headers || {}),
        },
        ...init,
    });

    if (res.status === 401) {
        if (opts?.ignore401) {
            // return a typed empty object or throw for caller to handle
            return {} as T;
        }
        try { sessionStorage.setItem("auth_expired", "1"); } catch { }
        window.location.replace("/");
        throw new Error("Session expired. Redirecting to sign in.");
    }

    const text = await res.text();
    let json: any = {};
    try { json = text ? JSON.parse(text) : {}; } catch { json = { raw: text }; }

    if (!res.ok) {
        const reason = json?.msg || json?.Reason || json?.error || res.statusText;
        throw new Error(typeof reason === "string" ? reason : `HTTP ${res.status}`);
    }
    return json as T;
}
export const Api = {
    // Users
    getUser: (id: string) => request<UserResponse>(`/api/users/${id}`, { method: "GET" }),
    createTempUser: (body: TmpUserRequest) => request<UserResponse>(`/api/users/new`, { method: "POST", body: JSON.stringify(body) }),
    register: (id: string, body: RegUserRequest) => request<UserResponse>(`/api/users/register/${id}`, { method: "POST", body: JSON.stringify(body) }),
    editAnon: (id: string, body: TmpUserRequest) => request<UserResponse>(`/api/users/edit/anon/${id}`, { method: "POST", body: JSON.stringify(body) }),
    editRegistered: (id: string, body: RegUserRequest) => request<UserResponse>(`/api/users/edit/registered/${id}`, { method: "POST", body: JSON.stringify(body) }),


    // Games
    createGame: (req: NewGameRequest) => request<NewGameResponse>(`/api/games/new`, { method: "POST", body: JSON.stringify(req) }),
    getGameState: (id: string) => request(`/api/games/id/${id}`, { method: "POST" }), // matches GamesController.Get
    meActiveGame: () =>
        request<GameCodeResponse>(
            `/api/games/me/active-game`,
            { method: "GET" },
            { ignore401: true }
        ),
    guestActiveGame: (userId: string) =>
        request<GameCodeResponse>(
            `/api/games/users/${userId}/active-game`,
            { method: "GET" }
        ),

};

export const Auth = {
    login: (body: { username: string; password: string }) =>
        request(`/api/auth/login`, { method: "POST", body: JSON.stringify(body) }),
    logout: () => request(`/api/auth/logout`, { method: "POST" }),
    me: () => request<UserResponse>(`/api/auth/me`, { method: "GET" }),
    meSafe: () => request<UserResponse>(`/api/auth/me`, { method: "GET" }, { ignore401: true }),
    register: (body: { tempUserId?: string; username: string; password: string; email?: string | null; iconUrl?: string | null }) =>
        request <UserResponse>(`/api/auth/register`, { method: "POST", body: JSON.stringify(body) }),
};



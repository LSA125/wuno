// Lightweight JSON client + typed helpers for your endpoints
import { RegUserRequest, TmpUserRequest, UserResponse, NewGameRequest, NewGameResponse } from "./types";


export async function request<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
    const res = await fetch(input, {
        credentials: "include", // <-- send/receive auth cookie
        headers: {
            "Content-Type": "application/json",
            "X-Requested-With": "XMLHttpRequest",
            ...(init?.headers || {}),
        },
        ...init,
    });

    // Handle auth expiry once, globally
    if (res.status === 401) {
        try {
            // mark a flag so the landing page can show "Session expired" toast
            sessionStorage.setItem("auth_expired", "1");
        } catch { }
        // hard redirect to kill any in-flight React state that may be causing loops
        window.location.replace("/");
        throw new Error("Session expired. Redirecting to sign in.");
    }

    // Some endpoints may return no content
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
};

export const Auth = {
    login: (body: { username: string; password: string }) =>
        request(`/api/auth/login`, { method: "POST", body: JSON.stringify(body) }),
    logout: () => request(`/api/auth/logout`, { method: "POST" }),
    me: () => request(`/api/auth/me`, { method: "GET" }),
    register: (body: { tempUserId?: string; username: string; password: string; email?: string | null; iconUrl?: string | null }) =>
        request(`/api/auth/register`, { method: "POST", body: JSON.stringify(body) }),
};



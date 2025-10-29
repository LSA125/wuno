// Lightweight JSON client + typed helpers for your endpoints
import { RegUserRequest, TmpUserRequest, UserResponse, NewGameRequest, NewGameResponse } from "./types";


async function request<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
    const res = await fetch(input, { headers: { "Content-Type": "application/json" }, ...init });
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
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";

// Use VITE_API_URL in production, empty string for local dev (Vite proxy handles it)
const API_BASE_URL = import.meta.env.VITE_API_URL || "";

export function createGameHub(): HubConnection {
    return new HubConnectionBuilder()
        .withUrl(`${API_BASE_URL}/hubs/game`, {
            withCredentials: true  // Required for cross-origin cookie auth
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();
}
import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { getAccessToken } from "@/api/client";

// Use VITE_API_URL in production, empty string for local dev (Vite proxy handles it)
const API_BASE_URL = import.meta.env.VITE_API_URL || "";

export function createGameHub(): HubConnection {
    // Get access token from localStorage (mobile fallback)
    const accessToken = getAccessToken();
    
    // Build URL with access token if available
    const hubUrl = accessToken 
        ? `${API_BASE_URL}/hubs/game?access_token=${encodeURIComponent(accessToken)}`
        : `${API_BASE_URL}/hubs/game`;
    
    return new HubConnectionBuilder()
        .withUrl(hubUrl, {
            withCredentials: true  // Also try cookie auth (works on desktop)
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();
}
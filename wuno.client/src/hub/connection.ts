import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";


export function createGameHub(): HubConnection {
    return new HubConnectionBuilder()
        .withUrl("/hubs/game") // Vite proxy forwards to ASP.NET Core
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Warning)
        .build();
}
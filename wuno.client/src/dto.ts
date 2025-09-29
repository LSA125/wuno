export interface NewGameRequest {
    playerCount?: number;   // default is 2
    targetWins?: number;    // default is 2
}

export interface NewGameResponse {
    gameId: string;  // Guid → string
    turnId: string;
    nextSeat: number;
    playerCount: number;
    targetWins: number;
}

export interface GameStateResponse {
    state: any;
}

export interface ErrorResponse {
    reason: string;
}

export interface SubmitWordRequest {
    seat: number;
    word: string;
}

export interface SubmitWordResponse {
    ok: boolean;
    reason?: string | null;
}

export interface JoinGameRequest {
    gameId: string;
}

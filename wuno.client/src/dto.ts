// Basic domain models
// types.ts

// Reusable primitives
export type Guid = string;
export type ISODateString = string;

// --- Enums (adjust values to match your C# enums exactly) ---
export enum GameStatus {
    WAITING = "WAITING",
    ACTIVE = "ACTIVE",
    FINISHED = "FINISHED",
}

export enum TurnEndReason {
    END = "END",
    TIMEOUT = "TIMEOUT",
}

export enum EffectType {
    ADD_TIME = "ADD_TIME",
    FREE_START = "FREE_START",
    ADJ_MIN_LEN = "ADJ_MIN_LEN",
    REQ_2_VOWELS = "REQ_2_VOWELS",
}

export enum EffectTarget {
    PREV = "PREV",
    SELF = "SELF",
    NEXT = "NEXT",
}
// --- Entities ---

export interface User {
    id: Guid;
    name: string;
    iconUrl?: string | null;
    email?: string | null;
    passwordHash?: string | null;
    createdAt: ISODateString;
    lastActiveAt: ISODateString;
    activePlayerId?: Guid | null;
    activePlayer?: Player | null; // optional to avoid cycles unless you hydrate it
}

export interface Game {
    id: Guid;
    code: string;
    status: GameStatus;
    targetWins: number;
    nextSeat: number;
    direction: number; // 1 = clockwise, -1 = counter-clockwise
    createdAt: ISODateString;

    players: Player[];
    rounds: Round[];
    turns: Turn[];
    effects: Effect[];
}

export interface Player {
    id: Guid;
    gameId: Guid;
    userId?: Guid | null;
    user?: User | null;

    name: string;
    iconUrl?: string | null;
    isActive: boolean;
    isConnected: boolean;
    isHost: boolean;
    seat: number;
    roundWins: number;
    lastWord?: string | null;
    lastWordLength: number;
}

export interface Round {
    id: Guid;
    gameId: Guid;
    index: number;
    active: boolean;
    winnerId?: Guid | null;
    startedAt?: ISODateString | null;
    endedAt?: ISODateString | null;

    // Navigation (optional to prevent circular payloads)
    game?: Game;
}

export interface Turn {
    id: Guid;
    gameId: Guid;
    roundId: Guid;
    index: number;
    seat: number;
    startLetter?: string | null; // was char? in C#
    minLen: number;
    require2Vowels: boolean;
    freeStart: boolean;
    startedAt: ISODateString;
    durationSec: number;
    word?: string | null;
    wordLen?: number | null;
    endedAt?: ISODateString | null;
    endReason?: TurnEndReason | null;

    // Navigation (optional)
    game?: Game;
    round?: Round;
}

export interface Effect {
    id: Guid;
    gameId: Guid;
    playerId: Guid;
    appliesOn: number; // recipient’s personal next turn index
    type: EffectType;
    value: number; // seconds or +/- length; bool as 0/1
    createdAt: ISODateString;

    // Navigation (optional)
    game?: Game;
}


// --- GameState and DTOs ---

export interface GameState {
    gameId: string;
    status: GameStatus;
    nextSeat: number;
    direction: number;
    targetWins: number;
    players: Player[];
    currentRound: Round;
    currentTurn: Turn;
}

// Requests & responses

export interface NewGameRequest {
    playerCount?: number;
    targetWins?: number;
}

export interface NewGameResponse {
    gameId: string;
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
    reason?: string;
}

export interface JoinGameRequest {
    gameId: string;
    userId: string;
}

export interface JoinGameResponse {
    gameId: string;
    state: GameState;
}

export interface LeaveGameRequest {
    gameId: string;
    playerId: string;
}

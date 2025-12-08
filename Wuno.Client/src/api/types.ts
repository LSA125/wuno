export type TmpUserRequest = { userId?: string; name: string; iconUrl?: string | null; email?: string | null };
export type RegUserRequest = { userId: string; pass: string; name?: string | null; iconUrl?: string | null; email?: string | null };
export type UserResponse = { ok: boolean; userId?: string | null; name?: string | null; iconUrl?: string | null; email?: string | null; msg?: string | null };
export type NewGameRequest = { playerCount: number; targetWins: number };
export type NewGameResponse = { gameCode: string; playerCount: number; targetWins: number };
export type GameCodeResponse = { ok: boolean; inGame: boolean | null; gameCode: string | null };

export type PlayerState = {
    playerId: string;
    seat: number;
    isActive: boolean;
    isConnected: boolean;
    name: string;
    iconUrl?: string | null;
    roundWins: number;
    lastWord?: string | null;
};

export type EffectState = { type: number; value: number };

export type TurnState = {
    turnId: string;
    index: number;
    seat: number;
    startedAt: string;
    dueAt: string;
    minLen: number;
    freeStart: boolean;
    effects: EffectState[];
};
export type TurnHistoryState = {
    turnId: string;
    index: number;
    seat: number;
    word: string;
    minLen: number;
    freeStart: boolean;
    effects: EffectState[];
};
export type RoundState = {
    roundId: string;
    index: number;
    winnerId?: string | null;
    startedAt?: string | null;
    endedAt?: string | null;
};

export type GameState = {
    gameId: string;
    status: number;
    nextSeat: number;
    direction: number;
    targetWins: number;
    lastWord?: string | null;
    players: PlayerState[];
    currentRound: RoundState | null;
    currentTurn: TurnState | null;
};
export type JoinGameResponse = { playerId: string; state: GameState };
export type SubmitWordResponse = { ok: boolean; reason?: string };
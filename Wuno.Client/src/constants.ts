// Game constants - keep in sync with wuno.domain/Constants.cs

// Timing constants
export const MIN_TURN_DUR_SEC = 3;
export const INITIAL_REMAINING_TIME_SEC = 30;
export const FIRST_TURN_MAX_TIME_SEC = 40;
export const TIME_DECREASE_PER_TURN_SEC = 3;
export const MAX_TIME_FLOOR_SEC = 5;
export const TIME_BONUS_MULTIPLIER = 0.5;
export const SCORE_DIVISOR = 3;
export const MIN_ACTUAL_TIME_SEC = 3;

// Player limits
export const MIN_PLAYERS = 2;
export const MAX_PLAYERS = 8;
export const MIN_TARGET_WINS = 1;
export const MAX_TARGET_WINS = 5;

// UI timing constants
export const TICK_INTERVAL_MS = 1000;
export const DANGER_THRESHOLD_MS = 4000;

// Sound constants
export const ERROR_SOUND_FREQ = 150;
export const ERROR_SOUND_DURATION = 0.12;
export const SUCCESS_SOUND_FREQ_1 = 523; // C5
export const SUCCESS_SOUND_FREQ_2 = 659; // E5
export const TURN_START_FREQ = 440;     // A4
export const TICK_FREQ = 800;
export const MATCH_BASE_FREQ = 260;
export const MATCH_FREQ_INCREMENT = 35;

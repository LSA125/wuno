import type { EffectState } from "@/api/types";
export enum EffectType { ADD_TIME = 0, FREE_START = 1, ADJ_MIN_LEN = 2, REQ_2_VOWELS = 3 }
export type EffectEvent = EffectState & { id: number };
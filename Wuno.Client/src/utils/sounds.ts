// Sound effects utility for game audio feedback
import {
    ERROR_SOUND_FREQ,
    ERROR_SOUND_DURATION,
    SUCCESS_SOUND_FREQ_1,
    SUCCESS_SOUND_FREQ_2,
    TURN_START_FREQ,
    TICK_FREQ,
    TICK_INTERVAL_MS,
    MATCH_BASE_FREQ,
    MATCH_FREQ_INCREMENT,
} from "@/constants";

let audioContext: AudioContext | null = null;

function getAudioContext(): AudioContext {
    if (!audioContext) {
        audioContext = new AudioContext();
    }
    return audioContext;
}

// Error/invalid sound - short buzz
export function playErrorSound() {
    try {
        const ctx = getAudioContext();
        const oscillator = ctx.createOscillator();
        const gain = ctx.createGain();
        
        oscillator.type = "square";
        oscillator.frequency.value = ERROR_SOUND_FREQ;
        gain.gain.value = 0.08;
        
        oscillator.connect(gain).connect(ctx.destination);
        oscillator.start();
        oscillator.stop(ctx.currentTime + ERROR_SOUND_DURATION);
    } catch (e) {
        console.warn("Audio error:", e);
    }
}

// Success sound - pleasant chime
export function playSuccessSound() {
    try {
        const ctx = getAudioContext();
        const oscillator = ctx.createOscillator();
        const gain = ctx.createGain();
        
        oscillator.type = "sine";
        oscillator.frequency.value = SUCCESS_SOUND_FREQ_1;
        gain.gain.value = 0.1;
        gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.3);
        
        oscillator.connect(gain).connect(ctx.destination);
        oscillator.start();
        oscillator.stop(ctx.currentTime + 0.3);
        
        // Second tone for melody
        setTimeout(() => {
            const osc2 = ctx.createOscillator();
            const gain2 = ctx.createGain();
            osc2.type = "sine";
            osc2.frequency.value = SUCCESS_SOUND_FREQ_2;
            gain2.gain.value = 0.08;
            gain2.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.25);
            osc2.connect(gain2).connect(ctx.destination);
            osc2.start();
            osc2.stop(ctx.currentTime + 0.25);
        }, 100);
    } catch (e) {
        console.warn("Audio error:", e);
    }
}

// Turn start sound - attention grabber
export function playTurnStartSound() {
    try {
        const ctx = getAudioContext();
        const oscillator = ctx.createOscillator();
        const gain = ctx.createGain();
        
        oscillator.type = "sine";
        oscillator.frequency.value = TURN_START_FREQ;
        gain.gain.value = 0.1;
        gain.gain.exponentialRampToValueAtTime(0.01, ctx.currentTime + 0.5);
        
        oscillator.connect(gain).connect(ctx.destination);
        oscillator.start();
        
        // Quick ascending tone
        oscillator.frequency.exponentialRampToValueAtTime(TURN_START_FREQ * 2, ctx.currentTime + 0.15);
        oscillator.stop(ctx.currentTime + 0.5);
    } catch (e) {
        console.warn("Audio error:", e);
    }
}

// Tick sound for bomb fuse
let tickInterval: ReturnType<typeof setInterval> | null = null;

export function startTickingSound() {
    stopTickingSound();
    
    const tick = () => {
        try {
            const ctx = getAudioContext();
            const oscillator = ctx.createOscillator();
            const gain = ctx.createGain();
            
            oscillator.type = "sine";
            oscillator.frequency.value = TICK_FREQ;
            gain.gain.value = 0.03;
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.05);
            
            oscillator.connect(gain).connect(ctx.destination);
            oscillator.start();
            oscillator.stop(ctx.currentTime + 0.05);
        } catch (e) {
            console.warn("Audio error:", e);
        }
    };
    
    tick();
    tickInterval = setInterval(tick, TICK_INTERVAL_MS);
}

export function stopTickingSound() {
    if (tickInterval) {
        clearInterval(tickInterval);
        tickInterval = null;
    }
}

// Match sound - gentle ascending tone when letter matches
export function playMatchSound(matchLength: number) {
    try {
        const ctx = getAudioContext();
        const oscillator = ctx.createOscillator();
        const gain = ctx.createGain();
        
        oscillator.type = "sine";
        oscillator.frequency.value = MATCH_BASE_FREQ + matchLength * MATCH_FREQ_INCREMENT;
        gain.gain.value = 0.05;
        
        oscillator.connect(gain).connect(ctx.destination);
        oscillator.start();
        oscillator.stop(ctx.currentTime + 0.15);
    } catch (e) {
        console.warn("Audio error:", e);
    }
}


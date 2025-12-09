import type { EffectState } from "@/api/types";
import { EffectType } from "./effectTypes";

type EffectChipProps = {
    effect: EffectState;
    floating?: boolean;
    subtle?: boolean;
    compact?: boolean;
};

function describeEffect(effect: EffectState) {
    switch (effect.type) {
        case EffectType.ADD_TIME: {
            const tone = effect.value >= 0 ? "good" : "bad";
            return { label: `${effect.value >= 0 ? "+" : ""}${effect.value}s`, tone };
        }
        case EffectType.ADJ_MIN_LEN: {
            const tone = effect.value <= 0 ? "good" : "bad";
            return { label: `${effect.value >= 0 ? "+" : ""}${effect.value} min`, tone };
        }
        case EffectType.FREE_START:
            return { label: "Free start", tone: "good" };
        default:
            return { label: "Effect", tone: "neutral" };
    }
}

export default function EffectChip({ effect, floating = false, subtle = false, compact = false }: EffectChipProps) {
    const { label, tone } = describeEffect(effect);
    const toneClass =
        tone === "good" ? "text-bg-success" : tone === "bad" ? "text-bg-danger" : "text-bg-info";
    const motionClass = floating ? "effect-float" : "effect-pulse";
    const subtleClass = subtle ? "effect-chip-subtle" : "";
    const compactClass = compact ? "effect-chip-compact" : "";
    return (
        <span className={`badge effect-chip ${toneClass} ${motionClass} ${subtleClass} ${compactClass}`} aria-label={label}>
            {label}
        </span>
    );
}
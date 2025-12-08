import type { EffectState } from "@/api/types";
import { computeReverseMatchLength, normalizeWord, reverseString } from "@/utils/wordMatching";
import EffectChip from "./EffectChip";

type WordPreviewProps = {
    word: string;
    previousWord?: string | null;
    minLen?: number;
    reverseMatchLength?: number;
    compact?: boolean;
    label?: string;
    effects?: EffectState[];
};

export default function WordPreview({
    word,
    previousWord,
    minLen = 0,
    reverseMatchLength,
    compact = false,
    label,
    effects = [],
}: WordPreviewProps) {
    const normalized = normalizeWord(word);
    const letters = normalized.toUpperCase().split("");
    const reversedPrev = reverseString(normalizeWord(previousWord || ""));
    const matched = reverseMatchLength ?? computeReverseMatchLength(normalized, reversedPrev);
    const activeLetters = Math.max(letters.length, minLen);

    return (
        <div className={`word-preview ${compact ? "word-preview-compact" : ""}`} aria-label={label || "Word preview"}>
            <div className="word-preview-letters" role="list">
                {letters.length === 0 && <span className="text-muted small">Waiting…</span>}
                {Array.from({ length: activeLetters }).map((_, idx) => {
                    const char = letters[idx];
                    const isRequired = idx < minLen;
                    const isMatched = idx < matched;
                    const hasChar = Boolean(char);

                    return (
                        <span
                            key={idx}
                            className={`word-letter ${isRequired ? "required" : ""} ${isMatched ? "match" : ""} ${hasChar ? "filled" : ""}`}
                            role="listitem"
                            aria-label={char || "Empty slot"}
                        >
                            {char || "·"}
                        </span>
                    );
                })}
            </div>
            {effects.length > 0 && (
                <div className="d-flex flex-wrap gap-1 mt-1" aria-label="Applied effects">
                    {effects.map((effect, idx) => (
                        <EffectChip key={`${effect.type}-${idx}`} effect={effect} subtle />
                    ))}
                </div>
            )}
        </div>
    );
}
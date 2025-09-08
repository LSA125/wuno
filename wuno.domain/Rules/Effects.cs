using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wuno.domain.Rules
{
    public record Constraints(char? StartLetter, int MinLen, int DurationSec, bool Require2Vowels, bool FreeStart)
    {
        public static Constraints Base(char? start) => new(start, Constants.DEFAULT_START_LEN, Constants.DEFAULT_TURN_DUR_SEC, false, false);
    }

    public static class EffectsLogic
    {
        public static IEnumerable<(EffectType type, int value, EffectTarget target)> SpecialsFromWord(string word, string? opponentsLast)
        {
            if (Words.IsPalindrome(word))
            {
                yield return (EffectType.ADD_TIME, +Constants.MID_TIME_ADJ_SEC, EffectTarget.SELF);
                yield return (EffectType.ADD_TIME, -Constants.MID_TIME_ADJ_SEC, EffectTarget.NEXT);
            }
            if (Words.HasLetter3Plus(word))
            {
                yield return (EffectType.ADJ_MIN_LEN, -Constants.LOW_LEN_ADJ, EffectTarget.SELF);
                yield return (EffectType.ADJ_MIN_LEN, +Constants.LOW_LEN_ADJ, EffectTarget.NEXT);
            }
            if (!string.IsNullOrEmpty(opponentsLast) && Words.IsAnagram(word, opponentsLast!))
            {
                yield return (EffectType.FREE_START, 1, EffectTarget.SELF);
            }
        }

        public static Constraints Apply(Constraints baseC, IEnumerable<(EffectType type, int value)> effects)
        {
            var c = baseC with { };
            foreach (var (type, val) in effects)
            {
                switch (type)
                {
                    case EffectType.ADD_TIME:
                        c = c with { DurationSec = Math.Clamp(c.DurationSec + val, 10, 45) }; break;
                    case EffectType.ADJ_MIN_LEN:
                        c = c with { MinLen = Math.Max(1, c.MinLen + val) }; break;
                    case EffectType.FREE_START:
                        if (val != 0) c = c with { FreeStart = true, StartLetter = null }; break;
                    case EffectType.REQ_2_VOWELS:
                        c = c with { Require2Vowels = val != 0 }; break;
                }
            }
            return c;
        }

        public static char? NextStartLetterFrom(string word) => Words.Last(word);
    }

}

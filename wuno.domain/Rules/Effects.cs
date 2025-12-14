using System.Security.Cryptography;
using System.Text;

namespace wuno.domain.Rules
{
    public sealed class EffectRng
    {
        private byte[] _buffer;
        private int _offset;

        public static EffectRng FromInputs(
            Guid gameId,
            int roundIndex,
            int turnNumber,
            string word,
            string? opponentsLast)
        {
            using var sha = SHA256.Create();
            var seedMaterial = $"{gameId:N}|{roundIndex}|{turnNumber}|{word}|{opponentsLast}";
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seedMaterial));
            return new EffectRng(bytes);
        }

        private EffectRng(byte[] seed)
        {
            _buffer = seed;
            _offset = 0;
        }
        private uint NextUInt32()
        {
            if (_offset + 4 > _buffer.Length)
            {
                using var sha = SHA256.Create();
                var more = sha.ComputeHash(_buffer);
                Array.Resize(ref _buffer, _buffer.Length + more.Length);
                Buffer.BlockCopy(more, 0, _buffer, _offset, more.Length);
            }

            uint val = BitConverter.ToUInt32(_buffer, _offset);
            _offset += 4;
            // simple xorshift to decorrelate consecutive pulls
            val ^= val << 13; val ^= val >> 17; val ^= val << 5;
            return val;
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) return 0;

            // Rejection sampling to avoid modulo bias
            uint limit = (uint.MaxValue / (uint)maxExclusive) * (uint)maxExclusive;
            uint r;
            do { r = NextUInt32(); } while (r >= limit);
            return (int)(r % (uint)maxExclusive);
        }
    }
    public record Constraints(char? StartLetter, int MinLen, int DurationSec, bool Require2Vowels, bool FreeStart)
    {
        public static Constraints Base(char? start, int turnNumber, string? lastWord) => 
            new(start, turnNumber + Constants.DEFAULT_START_LEN, 
                Math.Clamp(Constants.DEFAULT_TURN_DUR_SEC - Constants.DEFAULT_TIME_DECREASE_PER_TURN_SEC * turnNumber,
                    Constants.MIN_TURN_DUR_SEC,
                    Constants.MAX_TURN_DUR_SEC), 
                false, false);
    }
    public record EffectState(EffectType Type, int Value);

    public static class EffectsLogic
    {
        public static IEnumerable<(EffectType type, int value, EffectTarget target)> SpecialsFromWord
            (string word, 
            string? opponentsLast, 
            EffectRng rng)
        {
            int revMatchLength = Math.Max(0, Words.ReverseMatchLength(word, opponentsLast ?? "") - 1);

            for (int rev = revMatchLength; rev >= 1; rev--)
            {
                // pick target: 0=self, 1=opponent
                bool toSelf = rng.Next(2) == 0;

                // pick effect bucket: time or min-len, with sign depending on target
                bool timeEffect = rng.Next(2) == 0;

                if (toSelf && timeEffect)
                    yield return (EffectType.ADD_TIME, /*value*/ Constants.LOW_TIME_ADJ_SEC, EffectTarget.SELF);
                else if (toSelf && !timeEffect)
                    yield return (EffectType.ADJ_MIN_LEN, /*value*/ -Constants.LOW_LEN_ADJ, EffectTarget.SELF);
                else if (!toSelf && timeEffect)
                    yield return (EffectType.ADD_TIME, /*value*/ -Constants.LOW_TIME_ADJ_SEC, EffectTarget.NEXT);
                else
                    yield return (EffectType.ADJ_MIN_LEN, /*value*/ +Constants.LOW_LEN_ADJ, EffectTarget.NEXT);
            }
        }

        public static Constraints Apply(Constraints baseC, List<EffectState> effects)
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
                }
            }
            return c;
        }

        public static char? NextStartLetterFrom(string word) => Words.Last(word);
    }

}

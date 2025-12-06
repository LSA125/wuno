using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace wuno.domain.Rules
{
    public static class Words
    {
        static readonly HashSet<char> Vowels = new("aeiou");

        public static string Normalize(string w)
        {
            if (string.IsNullOrWhiteSpace(w))
                return string.Empty;

            var decomposed = w.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (var rune in decomposed.EnumerateRunes())
            {
                // strip accents
                if (Rune.GetUnicodeCategory(rune) == UnicodeCategory.NonSpacingMark)
                    continue;

                var lowerRune = Rune.ToLowerInvariant(rune);
                if (lowerRune.Value is >= 'a' and <= 'z')
                {
                    sb.Append((char)lowerRune.Value);
                }
            }

            return sb.ToString();
        }

        public static char? First(string w) { var s = Normalize(w); return s.Length == 0 ? null : s[0]; }
        public static char? Last(string w) { var s = Normalize(w); return s.Length == 0 ? null : s[^1]; }
        public static bool IsPalindrome(string w)
        {
            var s = Normalize(w); var r = s.Reverse().ToArray(); return s.SequenceEqual(r);
        }
        public static bool HasLetter3Plus(string w)
        {
            var s = Normalize(w); return s.GroupBy(c => c).Any(g => g.Count() >= 3);
        }
        public static bool IsAnagram(string a, string b)
        {
            var A = Normalize(a); var B = Normalize(b); if (A.Length != B.Length) return false;
            return A.OrderBy(c => c).SequenceEqual(B.OrderBy(c => c));
        }
        public static int VowelCount(string w) => Normalize(w).Count(c => Vowels.Contains(c));
        public static int ReverseMatchLength(string first, string second)
        {
            var A = Normalize(first); var B = Normalize(second);
            int len = 0; while (len < A.Length && len < B.Length && A[A.Length - 1 - len] == B[len]) len++;
            return len;
        }
    }

}

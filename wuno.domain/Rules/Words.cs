using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wuno.domain.Rules
{
    public static class Words
    {
        static readonly HashSet<char> Vowels = new("aeiou");

        public static string Normalize(string w) =>
          new string(w.ToLowerInvariant().Where(ch => ch is >= 'a' and <= 'z').ToArray());

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
        public static bool IsWord(string w) => Normalize(w).Length >= 2; // swap for real dict later
    }

}

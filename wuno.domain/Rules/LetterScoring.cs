namespace wuno.domain.Rules
{
    public static class LetterScoring
    {
        // Common (1pt): E,A,I,O,N,R,T,L,S,U
        // Uncommon (2pt): D,G,B,C,M,P,F,H,V,W,Y,K,J
        // Rare (5pt): X,Q,Z
        private static readonly Dictionary<char, int> LetterValues = new()
        {
            ['a'] = 1, ['e'] = 1, ['i'] = 1, ['o'] = 1, ['u'] = 1,
            ['n'] = 1, ['r'] = 1, ['t'] = 1, ['l'] = 1, ['s'] = 1,
            ['d'] = 2, ['g'] = 2, ['b'] = 2, ['c'] = 2, ['m'] = 2,
            ['p'] = 2, ['f'] = 2, ['h'] = 2, ['v'] = 2, ['w'] = 2,
            ['y'] = 2, ['k'] = 2, ['j'] = 2,
            ['x'] = 5, ['q'] = 5, ['z'] = 5
        };

        public static int GetLetterValue(char c) =>
            LetterValues.TryGetValue(char.ToLowerInvariant(c), out var val) ? val : 1;

        /// <summary>
        /// Score = sum of (letter_value * position_multiplier) for matching letters.
        /// Position 1 = 1x, position 2 = 2x, etc.
        /// </summary>
        public static int CalculateScore(string word, string? previousWord)
        {
            if (string.IsNullOrEmpty(word)) return 0;

            var normalizedWord = Words.Normalize(word);
            var normalizedPrev = Words.Normalize(previousWord ?? "");

            int matchLen = Words.ReverseMatchLength(normalizedPrev, normalizedWord);
            int score = 0;

            for (int i = 0; i < matchLen && i < normalizedWord.Length; i++)
            {
                score += GetLetterValue(normalizedWord[i]) * (i + 1);
            }
            return score;
        }
    }
}

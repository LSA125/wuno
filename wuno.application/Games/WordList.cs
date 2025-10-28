using FluentValidation.Internal;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using wuno.domain.Rules;

namespace Wuno.Application.Games
{
    public sealed class WordList : IWordList
    {
        FrozenSet<string> _words;
        public WordList()
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("Games.words.txt")
                ?? throw new InvalidOperationException("Word list not found in resources.");
            using var reader = new StreamReader(stream);

            _words = File.ReadLines("words.txt")
                .Where(l => !string.IsNullOrEmpty(l))
                .Select(Words.Normalize)
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }
        public bool IsWord(string word) => _words.Contains(Words.Normalize(word));
    }
}

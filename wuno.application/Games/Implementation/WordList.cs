using FluentValidation.Internal;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using wuno.domain.Rules;
using Wuno.Application.Games.Inheritance;

namespace Wuno.Application.Games.Implementation
{
    public sealed class WordList : IWordList
    {
        FrozenSet<string> _words;
        public WordList()
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = typeof(WordList).Assembly.GetManifestResourceStream(
                                   typeof(WordList), "words.txt")
                ?? throw new InvalidOperationException("Embedded words.txt not found.");
            using var reader = new StreamReader(stream);

            _words = reader.ReadToEnd()
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(Words.Normalize)
                .ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        }
        public bool IsWord(string word) => _words.Contains(Words.Normalize(word));
    }
}

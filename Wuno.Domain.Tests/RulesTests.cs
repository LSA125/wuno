using System;
using Wuno.Domain.Rules;
using wuno.domain.Rules;
using Xunit;

namespace Wuno.Domain.Tests
{
    public class RulesTests
    {
        [Theory]
        [InlineData("  Alice  ", "ALICE")]
        [InlineData("\t\n\r", "")]
        [InlineData("éclair", "ÉCLAIR")]
        public void Name_normalize_trims_and_uppercases(string input, string expected)
        {
            Assert.Equal(expected, Name.normalize(input));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("  user@Example.COM  ", "user@example.com")]
        [InlineData("   ", "")]
        public void NormalizeEmail_trims_lowercases_and_handles_null(string? input, string? expected)
        {
            Assert.Equal(expected, Email.NormalizeEmail(input));
        }

        [Theory]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("user@domain", false)]
        [InlineData("user@domain.com", true)]
        [InlineData("User+tag@Sub.Domain.IO", true)]
        public void LooksLikeEmail_detects_basic_formatting(string input, bool expected)
        {
            Assert.Equal(expected, Email.LooksLikeEmail(input));
        }

        [Theory]
        [InlineData("  Hello-World!  ", "helloworld")]
        [InlineData("HÉllo-123!", "hello")]
        [InlineData("   ", "")]
        [InlineData("😊Alpha", "alpha")]
        public void Words_normalize_removes_non_letters_and_lowercases(string input, string expected)
        {
            Assert.Equal(expected, Words.Normalize(input));
        }

        [Fact]
        public void Words_first_and_last_return_null_for_empty_input()
        {
            Assert.Null(Words.First("   \t"));
            Assert.Null(Words.Last(""));
        }

        [Fact]
        public void Words_first_and_last_use_normalized_letters()
        {
            Assert.Equal('a', Words.First(" Alpha"));
            Assert.Equal('o', Words.Last("HELLO!!"));
        }

        [Theory]
        [InlineData("level", true)]
        [InlineData("  deified  ", true)]
        [InlineData("abc", false)]
        [InlineData("😊", true)]
        public void Words_palindrome_detection_ignores_formatting(string input, bool expected)
        {
            Assert.Equal(expected, Words.IsPalindrome(input));
        }

        [Theory]
        [InlineData("tott", true)]
        [InlineData("banana", true)]
        [InlineData("pane", false)]
        [InlineData("", false)]
        public void Words_detect_letter_with_three_or_more_occurrences(string input, bool expected)
        {
            Assert.Equal(expected, Words.HasLetter3Plus(input));
        }

        [Theory]
        [InlineData("Listen", "Silent", true)]
        [InlineData("Triangle", "Integral", true)]
        [InlineData("Apple", "papel", true)]
        [InlineData("Apple", "pplea!", true)]
        [InlineData("Apple", "pplez", false)]
        public void Words_is_anagram_checks_normalized_comparison(string a, string b, bool expected)
        {
            Assert.Equal(expected, Words.IsAnagram(a, b));
        }

        [Fact]
        public void Words_vowel_count_uses_normalized_characters()
        {
            Assert.Equal(1, Words.VowelCount("cAt"));
            Assert.Equal(0, Words.VowelCount("rhythms"));
            Assert.Equal(1, Words.VowelCount("bÉd"));
        }

        [Theory]
        [InlineData("hello", "ole", 2)]
        [InlineData("abc", "xyz", 0)]
        [InlineData("", "world", 0)]
        [InlineData("civic", "civic", 5)]
        public void Words_reverse_match_length_counts_tail_to_head_overlap(string first, string second, int expected)
        {
            Assert.Equal(expected, Words.ReverseMatchLength(first, second));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Application.Games.Inheritance;

namespace Wuno.Application.Games
{
    public sealed class CodeGeneratorService : ICodeGeneratorService
    {
        private static readonly Random _random = new();
        public string GenerateCode()
        {
            
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude I, O, 1, 0 for clarity
            return new string(Enumerable.Repeat(chars, 6)
              .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    }
}

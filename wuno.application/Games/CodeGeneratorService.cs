using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.domain;
using wuno.infrastructure;

namespace Wuno.Application.Games
{
    public sealed class CodeGeneratorService : ICodeGeneratorService
    {
        private static readonly Random _random = new();
        AppDbContext _db;
        public CodeGeneratorService(AppDbContext db)
        {
            _db = db;
        }
        public string GenerateCode()
        {
            
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Exclude I, O, 1, 0 for clarity
            for(int i = 0; i<100; i++) // try up to 100 times to avoid collision
            {
                var code = new string(Enumerable.Repeat(chars, 6)
                  .Select(s => s[_random.Next(s.Length)]).ToArray());
                if (!_db.Games.Any(g => g.Code == code && g.Status != GameStatus.FINISHED))
                    return code;
            }
            throw new Exception("Failed to generate unique game code after 100 attempts.");
        }
    }
}

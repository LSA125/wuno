using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wuno.infrastructure;

namespace Wuno.Application.Games
{
    public interface ICodeGeneratorService
    {
        string GenerateCode();
    }
}

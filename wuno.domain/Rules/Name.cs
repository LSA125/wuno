using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wuno.Domain.Rules
{
    public class Name
    {
        public static string normalize(string name)
        {
            return name.Trim().ToUpperInvariant();
        }
    }
}

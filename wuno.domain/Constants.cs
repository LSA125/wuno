using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wuno.domain
{
    public static class Constants
    {
        public const int DEFAULT_START_LEN = 1;
        public const int MIN_TURN_DUR_SEC = 5;
        public const int MAX_TURN_DUR_SEC = 120;
        public const int DEFAULT_TURN_DUR_SEC = 30;
        public const int LOW_TIME_ADJ_SEC = 5;
        public const int MID_TIME_ADJ_SEC = 20;
        public const int HIGH_TIME_ADJ_SEC = 45;
        public const int LOW_LEN_ADJ = 1;
        public const int MED_LEN_ADJ = 2;
        public const int HIGH_LEN_ADJ = 3;
        public const int MIN_PLAYERS = 2;
        public const int MAX_PLAYERS = 8;
        public const int MIN_TARGET_WINS = 1;
        public const int MAX_TARGET_WINS = 5;
    }
}

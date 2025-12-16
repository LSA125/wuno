using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wuno.domain
{
    public static class Constants
    {
        public const int MIN_TURN_DUR_SEC = 3;
        public const int MIN_PLAYERS = 2;
        public const int MAX_PLAYERS = 8;
        public const int MIN_TARGET_WINS = 1;
        public const int MAX_TARGET_WINS = 5;

        // Timing formula constants
        public const int INITIAL_REMAINING_TIME_SEC = 30;
        public const int FIRST_TURN_MAX_TIME_SEC = 40;
        public const int TIME_DECREASE_PER_TURN_SEC = 3;
        public const int MAX_TIME_FLOOR_SEC = 5;
        public const double TIME_BONUS_MULTIPLIER = 0.5;
        public const int SCORE_DIVISOR = 3; // for min length calc
        public const int MIN_ACTUAL_TIME_SEC = 3;
    }
}

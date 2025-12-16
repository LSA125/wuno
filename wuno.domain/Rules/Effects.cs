namespace wuno.domain.Rules
{
    public record Constraints(char? StartLetter, int MinLen, int DurationSec);

    public static class EffectsLogic
    {
        /// <summary>
        /// Tmax = max(40 - 3*personalturns, 5) for first turn
        /// Tmax = max(25 - 3*personalturns, 5) for subsequent turns
        /// </summary>
        public static int CalculateMaxTime(int personalTurns, bool isFirstTurn = false)
        {
            int baseTime = isFirstTurn ? Constants.FIRST_TURN_MAX_TIME_SEC : 25;
            return Math.Max(baseTime - Constants.TIME_DECREASE_PER_TURN_SEC * personalTurns,
                           Constants.MAX_TIME_FLOOR_SEC);
        }

        /// <summary>
        /// Lrequired = max(floor(personalturns/3) - Lminremoved, 0)
        /// </summary>
        public static int CalculateMinLength(int personalTurns, int minLenRemoved) =>
            Math.Max(personalTurns / Constants.SCORE_DIVISOR - minLenRemoved, 0);

        /// <summary>
        /// Tbonus = score * 0.5
        /// Lminremoved = floor(score/3)
        /// </summary>
        public static (double timeBonus, int minLenRemoved) CalculateBonuses(int score) =>
            (score * Constants.TIME_BONUS_MULTIPLIER, score / Constants.SCORE_DIVISOR);

        /// <summary>
        /// Calculate debuffs for opponent based on excess bonuses:
        /// Tneg = max(Tmax - (Tremaining + score*0.5), 0)
        /// Ladd = max(floor(score/3) - Lrequired, 0)
        /// </summary>
        public static (double timeDebuff, int minLenDebuff) CalculateOpponentDebuffs(
            double remainingTime, int score, int tMax, int lRequired)
        {
            double selfTimeWithBonus = remainingTime + score * Constants.TIME_BONUS_MULTIPLIER;
            return (Math.Max(tMax - selfTimeWithBonus, 0),
                    Math.Max(score / Constants.SCORE_DIVISOR - lRequired, 0));
        }

        /// <summary>
        /// Tactual = max(min(Tlastremaining + Tbonus - Tneg, Tmax), 3)
        /// </summary>
        public static int CalculateActualTime(double lastRemaining, double timeBonus,
            double opponentTimeDebuff, int tMax) =>
            Math.Max(Math.Min((int)(lastRemaining + timeBonus - opponentTimeDebuff), tMax),
                    Constants.MIN_ACTUAL_TIME_SEC);

        public static char? NextStartLetterFrom(string word) => Words.Last(word);
    }
}

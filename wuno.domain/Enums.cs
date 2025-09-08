namespace wuno.domain
{
    public enum GameStatus {ACTIVE, FINISHED}
    public enum TurnEndReason {END, TIMEOUT}
    public enum EffectType { ADD_TIME, FREE_START, ADJ_MIN_LEN, REQ_2_VOWELS}
    public enum  EffectTarget { PREV, SELF, NEXT}
}

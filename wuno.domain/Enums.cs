namespace wuno.domain
{
    public enum GameStatus {WAITING, ACTIVE, FINISHED}
    public enum TurnEndReason {END, TIMEOUT}
    public enum EffectType { ADD_TIME, ADJ_MIN_LEN }
    public enum  EffectTarget { SELF, NEXT }
}

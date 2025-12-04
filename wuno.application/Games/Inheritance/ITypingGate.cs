namespace Wuno.Application.Games.Inheritance
{
    public interface ITypingGate
    {
        public bool tryAllow(string key, TimeSpan interval);
    }
}

using System.Collections.Concurrent;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Games.Util;

namespace Wuno.Testing.Fixtures
{
    public sealed class FakeTurnTimer : ITurnTimer
    {
        private readonly ConcurrentDictionary<Guid, ScheduledTurn> _scheduled = new();

        public IReadOnlyDictionary<Guid, ScheduledTurn> Scheduled => _scheduled;

        public bool Schedule(Guid gameId, Guid turnId, DateTime dueAtUTC, Func<GameState, Task> Broadcast)
        {
            return _scheduled.TryAdd(turnId, new ScheduledTurn(gameId, turnId, dueAtUTC, Broadcast));
        }

        public void Cancel(Guid turnId)
        {
            _scheduled.TryRemove(turnId, out _);
        }

        public async Task<int> RunDueAsync(DateTime utcNow, Func<Guid, Guid, Task<GameState?>> stateFactory)
        {
            var due = _scheduled.Values.Where(t => t.DueAtUTC <= utcNow).ToList();
            var tasks = due.Select(async t =>
            {
                if (!_scheduled.TryRemove(t.TurnId, out _)) return;
                var state = await stateFactory(t.GameId, t.TurnId);
                if (state is not null)
                {
                    await t.Broadcast(state);
                }
            });

            await Task.WhenAll(tasks);
            return due.Count;
        }

        public Task<int> AdvanceAsync(TimeSpan by, TestClock clock, Func<Guid, Guid, Task<GameState?>> stateFactory)
        {
            clock.Advance(by);
            return RunDueAsync(clock.UtcNow, stateFactory);
        }

        public record ScheduledTurn(Guid GameId, Guid TurnId, DateTime DueAtUTC, Func<GameState, Task> Broadcast);
    }
}

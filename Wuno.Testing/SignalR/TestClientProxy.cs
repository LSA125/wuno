using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Wuno.Testing.SignalR
{
    public sealed class TestClientProxy : IClientProxy
    {
        private readonly ConcurrentBag<HubInvocation> _invocations = new();

        public IReadOnlyCollection<HubInvocation> Invocations => _invocations;

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            _invocations.Add(new HubInvocation(method, args));
            return Task.CompletedTask;
        }
    }

    public sealed record HubInvocation(string Method, object?[] Args);
}

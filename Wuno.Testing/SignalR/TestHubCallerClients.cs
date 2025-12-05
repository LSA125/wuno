using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Wuno.Testing.SignalR
{
    public sealed class TestHubCallerClients : IHubCallerClients
    {
        private readonly ConcurrentDictionary<string, TestClientProxy> _targets = new();

        public TestClientProxy CallerProxy => (TestClientProxy)Caller;
        public TestClientProxy AllProxy => (TestClientProxy)All;
        public TestClientProxy OthersProxy => (TestClientProxy)Others;

        public IClientProxy All => GetProxy("all");
        public IClientProxy Caller => GetProxy("caller");
        public IClientProxy Others => GetProxy("others");

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
            => GetProxy($"allExcept:{string.Join(',', excludedConnectionIds)}");

        public IClientProxy Client(string connectionId)
            => GetProxy($"client:{connectionId}");

        public IClientProxy Clients(IReadOnlyList<string> connectionIds)
            => GetProxy($"clients:{string.Join(',', connectionIds)}");

        public IClientProxy Group(string groupName)
            => GetProxy($"group:{groupName}");

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
            => GetProxy($"groupExcept:{groupName}:{string.Join(',', excludedConnectionIds)}");

        public IClientProxy Groups(IReadOnlyList<string> groupNames)
            => GetProxy($"groups:{string.Join(',', groupNames)}");

        public IClientProxy OthersInGroup(string groupName)
            => GetProxy($"othersIn:{groupName}");

        public IClientProxy User(string userId)
            => GetProxy($"user:{userId}");

        public IClientProxy Users(IReadOnlyList<string> userIds)
            => GetProxy($"users:{string.Join(',', userIds)}");

        public TestClientProxy GetProxyForTarget(string targetKey) => _targets.GetOrAdd(targetKey, _ => new TestClientProxy());

        private TestClientProxy GetProxy(string target)
        {
            return _targets.GetOrAdd(target, _ => new TestClientProxy());
        }
    }
}

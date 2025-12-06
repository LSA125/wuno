using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Wuno.Testing.SignalR
{
    public sealed class TestHubCallerContext : HubCallerContext
    {
        private readonly string _connectionId;
        private readonly CancellationTokenSource _cts = new();
        private readonly string? _userIdentifier;
        private readonly ClaimsPrincipal? _user;
        private readonly IDictionary<object, object?> _items =
            new Dictionary<object, object?>();

        public TestHubCallerContext(string connectionId, ClaimsPrincipal? user = null, string? userIdentifier = null)
        {
            _connectionId = connectionId;
            _user = user ?? new ClaimsPrincipal(new ClaimsIdentity());
            _userIdentifier = userIdentifier;
        }

        public override string ConnectionId => _connectionId;
        public override string? UserIdentifier => _userIdentifier;
        public override ClaimsPrincipal? User => _user;
        public override IDictionary<object, object?> Items => _items;
        public override CancellationToken ConnectionAborted => _cts.Token;
        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override void Abort()
        {
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
        }
    }
}

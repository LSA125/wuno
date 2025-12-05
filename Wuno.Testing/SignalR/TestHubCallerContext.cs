using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Wuno.Testing.SignalR
{
    public sealed class TestHubCallerContext : HubCallerContext
    {
        private readonly string _connectionId;
        private readonly CancellationTokenSource _cts = new();

        public TestHubCallerContext(string connectionId, ClaimsPrincipal? user = null, string? userIdentifier = null)
        {
            _connectionId = connectionId;
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity());
            UserIdentifier = userIdentifier;
        }

        public override string ConnectionId => _connectionId;
        public override string? UserIdentifier { get; set; }
        public override ClaimsPrincipal User { get; set; }
        public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();
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

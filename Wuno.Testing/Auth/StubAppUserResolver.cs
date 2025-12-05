using Wuno.Application.Users;

namespace Wuno.Testing.Auth
{
    public sealed class StubAppUserResolver : IAppUserResolver
    {
        public Guid? UserId { get; set; }

        public StubAppUserResolver(Guid? userId = null)
        {
            UserId = userId;
        }

        public bool TryGet(out Guid userId)
        {
            if (UserId.HasValue)
            {
                userId = UserId.Value;
                return true;
            }

            userId = Guid.Empty;
            return false;
        }
    }
}

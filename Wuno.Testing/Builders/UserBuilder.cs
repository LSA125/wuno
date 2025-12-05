using wuno.domain;

namespace Wuno.Testing.Builders
{
    public sealed class UserBuilder
    {
        private Guid _id = Guid.NewGuid();
        private string? _name = "Test User";
        private string? _nameNormalized;
        private string? _iconUrl;
        private string? _email = "user@example.com";
        private string? _emailNormalized;
        private DateTime? _emailVerifiedAt;
        private string? _passwordHash;
        private bool _isRegistered;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _lastActiveAt = DateTime.UtcNow;
        private Guid? _activePlayerId;

        public UserBuilder WithId(Guid id) { _id = id; return this; }
        public UserBuilder WithName(string? name, string? normalized = null) { _name = name; _nameNormalized = normalized; return this; }
        public UserBuilder WithIcon(string? url) { _iconUrl = url; return this; }
        public UserBuilder WithEmail(string? email, string? normalized = null) { _email = email; _emailNormalized = normalized; return this; }
        public UserBuilder VerifiedAt(DateTime? at) { _emailVerifiedAt = at; return this; }
        public UserBuilder WithPassword(string? hash) { _passwordHash = hash; return this; }
        public UserBuilder Registered(bool registered = true) { _isRegistered = registered; return this; }
        public UserBuilder Created(DateTime created) { _createdAt = created; return this; }
        public UserBuilder LastActive(DateTime lastActive) { _lastActiveAt = lastActive; return this; }
        public UserBuilder WithActivePlayer(Guid? playerId) { _activePlayerId = playerId; return this; }

        public User Build()
        {
            return new User
            {
                Id = _id,
                Name = _name,
                NameNormalized = _nameNormalized,
                IconUrl = _iconUrl,
                Email = _email,
                EmailNormalized = _emailNormalized,
                EmailVerifiedAt = _emailVerifiedAt,
                PasswordHash = _passwordHash,
                IsRegistered = _isRegistered,
                CreatedAt = _createdAt,
                LastActiveAt = _lastActiveAt,
                ActivePlayerId = _activePlayerId,
            };
        }
    }
}

using Microsoft.EntityFrameworkCore;
using wuno.infrastructure;

namespace Wuno.Api.Services
{
    public sealed class GuestCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GuestCleanupService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24); // Run once per day
        private readonly TimeSpan _maxAge = TimeSpan.FromDays(30); // Delete guests older than 30 days

        public GuestCleanupService(IServiceScopeFactory scopeFactory, ILogger<GuestCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait a bit before first run to let app startup complete
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldGuestsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during guest cleanup");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task CleanupOldGuestsAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cutoff = DateTime.UtcNow - _maxAge;

            // Delete unregistered (guest) users who haven't been active in over 30 days
            // and are not currently in a game
            var oldGuests = await db.Users
                .Where(u => !u.IsRegistered)
                .Where(u => u.LastActiveAt < cutoff)
                .Where(u => u.ActivePlayerId == null) // Not in a game
                .ToListAsync(ct);

            if (oldGuests.Count > 0)
            {
                db.Users.RemoveRange(oldGuests);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Cleaned up {Count} old guest users", oldGuests.Count);
            }
        }
    }
}

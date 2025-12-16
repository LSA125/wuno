using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Api.Hubs;
using Wuno.Api.Middleware;
using Wuno.Application.Games.Implementation;
using Wuno.Application.Games.Inheritance;
using Wuno.Application.Users;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(opt =>
  opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IUserService, NoEmailUserService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAppUserResolver, AppUserResolver>();
builder.Services.AddScoped<IStatsService, StatsService>();

builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSingleton<ICodeGeneratorService, CodeGeneratorService>();
builder.Services.AddSingleton<ITypingGate, TypingGate>();
builder.Services.AddSingleton<ITurnTimer, TurnTimer>();
builder.Services.AddSingleton<IGroupTracker, GroupTracker>();
builder.Services.AddSingleton<IWordList, WordList>();
builder.Services.AddSingleton<IUserIdProvider, HubUserIdProvider>();
builder.Services.AddHostedService<Wuno.Api.Services.GuestCleanupService>();
builder.Services.AddControllers().AddJsonOptions(o => {
    o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});
builder.Services.AddRateLimiter(opts => {
    opts.AddFixedWindowLimiter("submit", o => {
        o.Window = TimeSpan.FromSeconds(10);
        o.PermitLimit = 5;
        o.QueueLimit = 0;
    });
});
builder.Services.AddCors(o =>
{
    o.AddPolicy("spa", p => p
        .WithOrigins(
            "https://localhost:5173", // SPA
            "https://localhost:7031"  // API (if you call it directly sometimes)
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(o =>
  {
      o.Cookie.Name = "wuno_auth";
      o.Cookie.HttpOnly = true;
#if DEBUG
      o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
#else
      o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
#endif
      o.Cookie.SameSite = SameSiteMode.None;
      o.SlidingExpiration = true;
      o.ExpireTimeSpan = TimeSpan.FromDays(30);

      o.Events = new CookieAuthenticationEvents
      {
          OnRedirectToLogin = ctx =>
          {
              if (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/hubs"))
              {
                  ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                  return Task.CompletedTask;
              }
              ctx.Response.Redirect(ctx.RedirectUri);
              return Task.CompletedTask;
          }
      };
  });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection()
  .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
  .SetApplicationName("WunoApp");


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("spa");
app.UseMiddleware<EnsureGuestCookieMiddleware>();
app.UseAuthentication();
app.UseAuthorization();


app.Use(async (ctx, next) =>
{
    if (HttpMethods.IsPost(ctx.Request.Method) ||
        HttpMethods.IsPut(ctx.Request.Method) ||
        HttpMethods.IsPatch(ctx.Request.Method) ||
        HttpMethods.IsDelete(ctx.Request.Method))
    {
        if (ctx.Request.Headers["X-Requested-With"] != "XMLHttpRequest")
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsync("Bad request");
            return;
        }
    }
    await next();
});

app.UseHttpsRedirection();

app.MapHub<GameHub>("/hubs/game");
app.UseRateLimiter();



app.MapControllers();

app.Run();
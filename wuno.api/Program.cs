using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using wuno.domain;
using wuno.infrastructure;
using Wuno.Api.Hubs;
using Wuno.Application.Games;
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
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSingleton<ICodeGeneratorService, CodeGeneratorService>();
builder.Services.AddSingleton<ITypingGate, TypingGate>();
builder.Services.AddSingleton<ITurnTimer, TurnTimer>();
builder.Services.AddSingleton<IGroupTracker, GroupTracker>();
builder.Services.AddSingleton<IWordList, WordList>();
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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
  .AddCookie(o =>
  {
      o.Cookie.Name = "wuno_auth";
      o.Cookie.HttpOnly = true;
      o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
      o.Cookie.SameSite = SameSiteMode.Lax; // works with top-level navigations, protects CSRF reasonably for SPA
      o.SlidingExpiration = true;
      o.ExpireTimeSpan = TimeSpan.FromDays(14);
      o.Events = new CookieAuthenticationEvents
      {
          OnCheckSlidingExpiration = context =>
          {
              // Extend expiration only if more than half the time has passed
              var issuedUtc = context.Properties.IssuedUtc;
              if (issuedUtc.HasValue)
              {
                  var timeElapsed = DateTimeOffset.UtcNow - issuedUtc.Value;
                  if (timeElapsed > TimeSpan.FromDays(7))
                  {
                      context.ShouldRenew = true;
                  }
              }
              return Task.CompletedTask;
          }
      };
  });

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddCors();
builder.Services.AddDataProtection()
  .PersistKeysToFileSystem(new DirectoryInfo(
      Path.Combine(builder.Environment.ContentRootPath, "keys")))
  .SetApplicationName("WunoApp");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
app.UseCors(p => p.WithOrigins("https://localhost:5173", "http://localhost:3000", "https://localhost:7031", "http://localhost:5139")
                  .AllowAnyHeader().AllowAnyMethod());
app.MapHub<GameHub>("/hubs/game");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();

app.Run();
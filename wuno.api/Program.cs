using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using wuno.infrastructure;
using Wuno.Api.Hubs;
using Wuno.Application.Games;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(opt =>
  opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IGameService, GameService>();
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
builder.Services.AddSignalR();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(p => p.WithOrigins("https://localhost:5173", "http://localhost:3000", "https://localhost:7031", "http://localhost:5139")
                  .AllowAnyHeader().AllowAnyMethod());
app.MapHub<GameHub>("hubs/game");
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();

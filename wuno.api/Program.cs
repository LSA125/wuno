using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System;
using wuno.infrastructure;
using Wuno.Api.Background;
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
        o.Window = TimeSpan.FromSeconds(1);
        o.PermitLimit = 5;
        o.QueueLimit = 0;
    });
});
builder.Services.AddHostedService<TurnSweeper>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(p => p.WithOrigins("http://localhost:5139", "http://localhost:3000", "https://localhost:7031", "http://localhost:5139")
                  .AllowAnyHeader().AllowAnyMethod());
app.MapHub<GameHub>("hub");
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();

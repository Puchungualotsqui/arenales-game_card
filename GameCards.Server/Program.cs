using GameCards.Server.Services;
using GameCards.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GameManager>();
builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:5170") // Blazor client dev port
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // needed for SignalR
    });
});

var app = builder.Build();

app.UseCors("AllowClient");

app.MapControllers();
app.MapHub<GameHub>("/gamehub");

app.Run();
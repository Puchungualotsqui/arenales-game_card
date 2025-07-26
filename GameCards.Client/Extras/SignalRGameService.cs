using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using GameCards.Shared;

namespace GameCards.Client.Extras;
    
public class SignalRGameService : IAsyncDisposable
{
    private readonly NavigationManager _navManager;
    private HubConnection? _hubConnection;

    public GamePublicState? CurrentState { get; private set; }
    public event Action<GamePublicState>? OnGameStateUpdated;
    public event Action<string, GamePublicState>? OnPlayerJoined;
    public event Action<string, GamePublicState>? OnPlayerLeft;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    

    public async Task<GameCreatedDto> CreateGame(string ownerPlayerId, bool isPublic, int maxPlayers)
    {
        if (!IsConnected) await StartAsync();
        return await _hubConnection!.InvokeAsync<GameCreatedDto>(
            "CreateGame", ownerPlayerId, isPublic, maxPlayers);
    }
    
    public async Task<Guid?> JoinAnyAvailableGame(string playerId, string playerName)
    {
        if (!IsConnected) await StartAsync();
        return await _hubConnection!.InvokeAsync<Guid?>("JoinAnyAvailableGame", playerId, playerName);
    }

    public SignalRGameService(NavigationManager navManager)
    {
        _navManager = navManager;
    }

    public async Task StartAsync()
    {
        // If connection exists but is closed → dispose and rebuild
        if (_hubConnection != null && _hubConnection.State != HubConnectionState.Connected)
        {
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        // Build a new connection if needed
        if (_hubConnection == null)
        {
            var hubUrl = _navManager.BaseUri.StartsWith("https", StringComparison.OrdinalIgnoreCase)
                ? "https://localhost:5026/gamehub"
                : "http://localhost:5026/gamehub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect() // ✅ auto-reconnect on temporary network drops
                .Build();

            // ✅ Reattach listeners
            _hubConnection.On<GamePublicState>("GameStateUpdated", state =>
            {
                Console.WriteLine("📡 Received GameStateUpdated");
                CurrentState = state;
                OnGameStateUpdated?.Invoke(state);
            });

            _hubConnection.On<string, GamePublicState>("PlayerJoined", (playerName, state) =>
            {
                Console.WriteLine($"📡 Received PlayerJoined: {playerName}");
                CurrentState = state;
                OnPlayerJoined?.Invoke(playerName, state);
            });

            _hubConnection.On<string, GamePublicState>("PlayerLeft", (playerName, state) =>
            {
                Console.WriteLine($"📡 Received PlayerLeft: {playerName}");
                CurrentState = state;
                OnPlayerLeft?.Invoke(playerName, state);
            });
        }

        // Start if not running
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            Console.WriteLine("🔄 Connecting to SignalR hub...");
            await _hubConnection.StartAsync();
            Console.WriteLine("✅ SignalR connected");
        }
    }

    public async Task JoinGame(Guid gameId, string playerId, string playerName)
    {
        if (!IsConnected) await StartAsync();
        await _hubConnection!.InvokeAsync("JoinGame", gameId, playerId, playerName);
    }
    
    public async Task LeaveGame(Guid gameId, string playerId)
    {
        if (!IsConnected) await StartAsync();
        await _hubConnection!.InvokeAsync("LeaveGame", gameId, playerId);
    }

    public async Task StartGame(Guid gameId)
    {
        if (!IsConnected) return;
        await _hubConnection!.InvokeAsync("StartGame", gameId);
    }

    public async Task AdvanceTurn(Guid gameId)
    {
        if (!IsConnected) return;
        await _hubConnection!.InvokeAsync("AdvanceTurn", gameId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
    
    public event Action<string, GamePublicState>? OnPlayerKicked;   // owner kicked someone
    public event Action<Guid>? OnYouWereKicked;                    // you got kicked
    public event Action<Guid>? OnGameStarted;

    public async Task KickPlayer(Guid gameId, string ownerPlayerId, string targetPlayerId)
    {
        if (!IsConnected) await StartAsync();
        await _hubConnection!.InvokeAsync("KickPlayer", gameId, ownerPlayerId, targetPlayerId);
    }
    
    
}
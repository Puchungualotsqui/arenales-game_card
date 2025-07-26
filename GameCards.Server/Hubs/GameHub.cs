using GameCards.Server.Controllers;
using Microsoft.AspNetCore.SignalR;
using GameCards.Server.Services;
using GameCards.Shared;

namespace GameCards.Server.Hubs;

public class GameHub : Hub
{
    private readonly GameManager _gameManager;
    
    private static readonly Dictionary<string, string> PlayerConnections = new(); 
    // key: playerId, value: connectionId

    
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"✅ New client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"❌ Client disconnected: {Context.ConnectionId}");

        // ✅ Find which player was using this connection
        var player = PlayerConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;

        if (!string.IsNullOrEmpty(player))
        {
            // Find their current game
            if (Guid.TryParse(player, out var guid) && UserController.Users.TryGetValue(guid, out var profile))
            {
                if (profile.CurrentGameId.HasValue)
                {
                    var gameId = profile.CurrentGameId.Value;
                    Console.WriteLine($"🛑 Removing player {player} from game {gameId} due to disconnect");

                    // Call LeaveGame logic directly
                    await LeaveGame(gameId, player);
                }
            }

            // Remove from connection map
            PlayerConnections.Remove(player);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public GameHub(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    
    public async Task<GameCreatedDto> CreateGame(string playerId, bool isPublic, int maxPlayers)
    {
        var userGuid = Guid.Parse(playerId);
        UserController.Users.TryGetValue(userGuid, out var userProfile);

        // ✅ If already in a game → return existing
        if (userProfile?.CurrentGameId.HasValue == true)
        {
            var existingGame = _gameManager.GetGame(userProfile.CurrentGameId.Value);
            if (existingGame != null)
            {
                await LeaveGame(userProfile.CurrentGameId.Value, playerId);
            }
        }

        // ✅ Create a new game
        var game = _gameManager.CreateGame(playerId, isPublic, maxPlayers);

        // Owner auto-joins
        game.AddPlayer(playerId, userProfile?.DisplayName ?? "Guest");

        if (userProfile != null)
            userProfile.CurrentGameId = game.GameId;

        // ✅ Add creator's current connection to the SignalR group immediately
        await Groups.AddToGroupAsync(Context.ConnectionId, game.GameId.ToString());
        Console.WriteLine($"✅ Game Created: {game.GameId.ToString()}");
        return ToGameCreatedDto(game);
    }


// ✅ Helper to build DTO
    private GameCreatedDto ToGameCreatedDto(ArenalesGame game)
    {
        return new GameCreatedDto
        {
            GameId = game.GameId,
            OwnerPlayerId = game.OwnerPlayerId,
            IsPublic = game.IsPublic,
            MaxPlayers = game.MaxPlayers,
            Players = game.Players.Select(p => new LobbyPlayerDto
            {
                PlayerId = p.PlayerId,
                DisplayName = p.Name
            }).ToList()
        };
    }


    public async Task JoinGame(Guid gameId, string playerId, string playerName)
    {
        var game = _gameManager.GetGame(gameId);
        if (game == null)
            throw new Exception("Game not found");

        if (!game.Players.Any(p => p.PlayerId == playerId))
            game.AddPlayer(playerId, playerName);

        // ✅ Link user to this game
        if (Guid.TryParse(playerId, out var guid) && UserController.Users.TryGetValue(guid, out var profile))
        {
            profile.CurrentGameId = gameId;
        }
        
        PlayerConnections[playerId] = Context.ConnectionId;

        await Groups.AddToGroupAsync(Context.ConnectionId, gameId.ToString());

        await Clients.Caller.SendAsync("GameStateUpdated", game.GetPublicState());

        await Clients.Group(gameId.ToString())
            .SendAsync("PlayerJoined", playerName, game.GetPublicState());
        
        Console.WriteLine($"👥 Player joined: {playerName} | Game {gameId} now has {game.Players.Count} players.");
    }
    
    public async Task<Guid?> JoinAnyAvailableGame(string playerId, string playerName)
    {
        if (Guid.TryParse(playerId, out var guid) && 
            UserController.Users.TryGetValue(guid, out var profile) &&
            profile.CurrentGameId.HasValue)
        {
            return profile.CurrentGameId.Value;
        }

        var publicGame = _gameManager.FindFirstPublicWaitingGame();
        if (publicGame == null)
            return null;

        await JoinGame(publicGame.GameId, playerId, playerName);
        return publicGame.GameId;
    }

    public async Task StartGame(Guid gameId)
    {
        var game = _gameManager.GetGame(gameId);
        if (game == null)
            throw new Exception("Game not found");

        if (game.IsStarted)
            return; // already in progress

        game.StartGame();

        // ✅ Broadcast GameStateUpdated if needed
        await Clients.Group(gameId.ToString())
            .SendAsync("GameStateUpdated", game.GetPublicState());

        // ✅ Broadcast new event to redirect players
        await Clients.Group(gameId.ToString())
            .SendAsync("GameStarted", gameId);
    }

    
    public async Task AdvanceTurn(Guid gameId)
    {
        var game = _gameManager.GetGame(gameId);
        if (game == null) throw new Exception("Game not found");
        
        game.AdvanceTurnPhase();
        
        await Clients.Group(gameId.ToString())
            .SendAsync("GameStateUpdated", game.GetPublicState());
    }
    
    public async Task LeaveGame(Guid gameId, string playerId)
    {
        var game = _gameManager.GetGame(gameId);
        if (game == null)
            return; // Already removed

        // Remove from SignalR group
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.ToString());

        // Find player
        var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null)
            return; // Not in the game

        // Remove from backend state
        game.Players.Remove(player);

        // Clear CurrentGameId in user profile
        if (Guid.TryParse(playerId, out var guid) && UserController.Users.TryGetValue(guid, out var profile))
        {
            profile.CurrentGameId = null;
        }

        // Case 1: Game is now empty → destroy
        if (!game.Players.Any())
        {
            Console.WriteLine($"🗑 Game {gameId} destroyed (last player left)");
            _gameManager.RemoveGame(gameId);
            return;
        }

        // Case 2: If leaving player was the owner → assign a new owner
        if (game.OwnerPlayerId == playerId)
        {
            var newOwner = game.Players.First();
            game.OwnerPlayerId = newOwner.PlayerId;
            Console.WriteLine($"👑 New owner is {newOwner.Name}");
        }

        // Notify remaining players that someone left
        await Clients.Group(gameId.ToString())
            .SendAsync("PlayerLeft", player.Name, game.GetPublicState());
    }

    public async Task KickPlayer(Guid gameId, string ownerPlayerId, string targetPlayerId)
    {
        var game = _gameManager.GetGame(gameId);
        if (game == null) return;

        // Only allow current owner to kick
        if (game.OwnerPlayerId != ownerPlayerId)
        {
            Console.WriteLine($"❌ Kick attempt denied. {ownerPlayerId} is not owner of {gameId}");
            return;
        }

        var target = game.Players.FirstOrDefault(p => p.PlayerId == targetPlayerId);
        if (target == null) return;

        Console.WriteLine($"🚨 Owner {ownerPlayerId} kicked {targetPlayerId} from {gameId}");

        // Remove the target from the game
        game.Players.Remove(target);

        // Clear CurrentGameId for kicked player
        if (Guid.TryParse(targetPlayerId, out var kickedGuid) && UserController.Users.TryGetValue(kickedGuid, out var profile))
            profile.CurrentGameId = null;

        // If only one left (owner), game still alive
        // If game empty after kick → destroy it
        if (!game.Players.Any())
        {
            Console.WriteLine($"🗑 Game {gameId} destroyed after last player was kicked");
            _gameManager.RemoveGame(gameId);
        }

        // Notify all players in the group
        await Clients.Group(gameId.ToString())
            .SendAsync("PlayerKicked", target.PlayerId, game.GetPublicState());

        // ✅ Now find the kicked player’s connection id
        if (PlayerConnections.TryGetValue(targetPlayerId, out var kickedConnectionId))
        {
            await Clients.Client(kickedConnectionId).SendAsync("YouWereKicked", gameId);

            // Remove them from SignalR group
            await Groups.RemoveFromGroupAsync(kickedConnectionId, gameId.ToString());
        }
    }
}
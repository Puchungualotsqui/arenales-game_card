namespace GameCards.Server.Services;

public class GameManager
{
    private readonly Dictionary<Guid, ArenalesGame> _activeGames = new Dictionary<Guid, ArenalesGame>();

    public ArenalesGame CreateGame(string ownerPlayerId, bool isPublic, int maxPlayers)
    {
        var game = new ArenalesGame {IsPublic = isPublic, MaxPlayers = maxPlayers, OwnerPlayerId = ownerPlayerId};
        _activeGames[game.GameId] = game;
        return game;
    }

    public ArenalesGame? GetGame(Guid gameId)
    {
        _activeGames.TryGetValue(gameId, out var game);
        return game;
    }
    
    public ArenalesGame? FindFirstPublicWaitingGame()
    {
        return _activeGames.Values.FirstOrDefault(g => g.IsPublic & !g.IsStarted);
    }
    
    public ArenalesGame? FindGameByOwner(string ownerPlayerId)
    {
        return _activeGames.Values.FirstOrDefault(g => g.OwnerPlayerId == ownerPlayerId && !g.IsStarted);
    }
    
    public void RemoveGame(Guid gameId)
    {
        if (_activeGames.ContainsKey(gameId))
            _activeGames.Remove(gameId);
    }


}
namespace GameCards.Shared;

public enum GamePhase // overall game status
{
    WaitingForPlayers,
    Setup,
    InProgress,
    GameOver
}

public enum TurnPhase // current player’s internal phase
{
    Action,
    Buy,
    Cleanup
}

public record PlayerPublicView(
    string PlayerId,
    string PlayerName,
    List<CardStruct> DiscardPile,
    List<CardStruct> DeckCards,
    List<CardStruct> HandCards);
    
public record GamePublicState(
    Guid GameId,
    string OwnerPlayerId,
    GamePhase GamePhase,         // Overall game state
    TurnPhase CurrentTurnPhase,  // Current player's phase
    string PlayerPlayingId,      // Active player's ID
    List<PlayerPublicView> Players,
    int CurrentTurnIndex,        // Which player is active
    int TurnNumber               // How many rounds have passed
    );
    
public class GameCreatedDto
{
    public Guid GameId { get; set; }
    public string OwnerPlayerId { get; set; } = "";
    public bool IsPublic { get; set; }
    public int MaxPlayers { get; set; }
    public List<LobbyPlayerDto> Players { get; set; } = new();
}

public class LobbyPlayerDto
{
    public string PlayerId { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
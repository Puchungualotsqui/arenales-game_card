namespace GameCards.Shared;

public class GameInfoDto
{
    public Guid GameId { get; set; }
    public string OwnerPlayerId { get; set; } = "";
    public bool IsPublic { get; set; }
    public int MaxPlayers { get; set; }
    public List<PlayerPublicInfo> Players { get; set; } = new();
}

public class PlayerPublicInfo
{
    public string PlayerId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? AvatarUrl { get; set; }
}

public class UpdatePrivacyDto
{
    public Guid GameId { get; set; }
    public bool IsPublic { get; set; }
}

public class UpdateMaxPlayersDto
{
    public Guid GameId { get; set; }
    public int MaxPlayers { get; set; }
}

public class StartGameDto
{
    public Guid GameId { get; set; }
}
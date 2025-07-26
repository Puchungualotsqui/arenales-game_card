using GameCards.Server.Services;
using GameCards.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GameCards.Server.Controllers;

[ApiController]
[Route("api/game")]
public class GameController : ControllerBase
{
    private readonly GameManager _gameManager;

    public GameController(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    // ✅ Return a strongly typed DTO instead of anonymous object
    [HttpGet("info/{gameId}")]
    public ActionResult<GameInfoDto> GetGameInfo(Guid gameId)
    {
        var game = _gameManager.GetGame(gameId);
        if (game == null)
            return NotFound();

        return Ok(ToGameInfoDto(game));
    }

    // ✅ Update game privacy using shared DTO
    [HttpPost("update-privacy")]
    public IActionResult UpdatePrivacy(UpdatePrivacyDto dto)
    {
        var game = _gameManager.GetGame(dto.GameId);
        if (game == null)
            return NotFound();

        game.IsPublic = dto.IsPublic;
        return Ok();
    }

    // ✅ Update max players using shared DTO
    [HttpPost("update-maxplayers")]
    public IActionResult UpdateMaxPlayers(UpdateMaxPlayersDto dto)
    {
        var game = _gameManager.GetGame(dto.GameId);
        if (game == null)
            return NotFound();

        game.MaxPlayers = dto.MaxPlayers;
        return Ok();
    }

    // ✅ Start game using shared DTO
    [HttpPost("start")]
    public IActionResult StartGame(StartGameDto dto)
    {
        var game = _gameManager.GetGame(dto.GameId);
        if (game == null)
            return NotFound();

        game.StartGame();
        return Ok();
    }

    // ✅ Helper: map ArenalesGame -> GameInfoDto
    private GameInfoDto ToGameInfoDto(ArenalesGame game)
    {
        var dto = new GameInfoDto
        {
            GameId = game.GameId,
            OwnerPlayerId = game.OwnerPlayerId,
            IsPublic = game.IsPublic,
            MaxPlayers = game.MaxPlayers,
            Players = game.Players.Select(p =>
            {
                var displayName = p.Name;
                string? avatarUrl = null;

                // Look up from UserController.Users to enrich info
                if (Guid.TryParse(p.PlayerId, out var pid) &&
                    UserController.Users.TryGetValue(pid, out var profile))
                {
                    displayName = profile.DisplayName;
                    avatarUrl = profile.AvatarUrl;
                }

                return new PlayerPublicInfo
                {
                    PlayerId = p.PlayerId,
                    DisplayName = displayName,
                    AvatarUrl = avatarUrl
                };
            }).ToList()
        };

        return dto;
    }
}

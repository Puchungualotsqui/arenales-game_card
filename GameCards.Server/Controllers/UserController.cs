using GameCards.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GameCards.Server.Controllers;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    public static readonly Dictionary<Guid, UserProfile> Users = new();

    [HttpPost("register")]
    public IActionResult Register([FromBody] UserProfile profile)
    {
        Users[profile.PlayerId] = profile;
        Console.WriteLine($"Registered guest {profile.PlayerId}");
        return Ok();
    }
    
    [HttpGet("{playerId}")]
    public IActionResult GetUser(Guid playerId)
    {
        Console.WriteLine($"📡 GET /api/user/{playerId} called");

        if (Users.TryGetValue(playerId, out var user))
        {
            Console.WriteLine($"✅ Found user {user.DisplayName} ({user.Email ?? "Guest"})");
            return Ok(user);
        }

        Console.WriteLine($"❌ User {playerId} NOT found in backend");
        return NotFound();
    }
    
    [HttpGet("{playerId}/current-game")]
    public IActionResult GetCurrentGame(Guid playerId)
    {
        Console.WriteLine($"📡 GET /api/user/{playerId}/current-game called");

        if (Users.TryGetValue(playerId, out var user))
        {
            Console.WriteLine(user.CurrentGameId.HasValue
                ? $"✅ User is in game {user.CurrentGameId}"
                : "✅ User is not in any game");

            return Ok(user.CurrentGameId); // Will return `null` or a Guid
        }

        Console.WriteLine($"❌ User {playerId} NOT found in backend");
        return NotFound();
    }
}
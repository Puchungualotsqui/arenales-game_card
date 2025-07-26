using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using GameCards.Shared;

namespace GameCards.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    // 1. Redirect user to Google OAuth
    [HttpGet("google-login")]
    public IActionResult GoogleLogin([FromQuery] Guid playerId)
    {
        var clientId = _config["GoogleAuth:ClientId"];
        var redirectUri = _config["GoogleAuth:RedirectUri"];
        var scope = "openid email profile";

        var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth" +
                            $"?client_id={clientId}" +
                            $"&redirect_uri={redirectUri}" +
                            $"&response_type=code" +
                            $"&scope={Uri.EscapeDataString(scope)}" +
                            $"&state={playerId}"; // pass through

        Console.WriteLine("Redirecting to Google: " + googleAuthUrl);
        return Redirect(googleAuthUrl);
    }

    // 2. Handle Google redirect & exchange code for token
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        Console.WriteLine("🔄 GoogleCallback called");
        Console.WriteLine($"state={state}, code={code}");

        if (!Guid.TryParse(state, out Guid playerId))
        {
            Console.WriteLine("❌ Invalid playerId from state");
            return BadRequest("Invalid player ID in Google login state.");
        }

        var clientId = _config["GoogleAuth:ClientId"];
        var clientSecret = _config["GoogleAuth:ClientSecret"];
        var redirectUri = _config["GoogleAuth:RedirectUri"];

        var http = _httpClientFactory.CreateClient();

        Console.WriteLine("🔄 Exchanging code for token...");
        var tokenResponse = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"code", code},
                {"client_id", clientId},
                {"client_secret", clientSecret},
                {"redirect_uri", redirectUri},
                {"grant_type", "authorization_code"}
            }));

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        Console.WriteLine("Google token response: " + tokenJson);

        var tokenData = JsonDocument.Parse(tokenJson).RootElement;

        if (!tokenData.TryGetProperty("access_token", out var accessTokenProp))
        {
            Console.WriteLine("❌ No access_token in token response");
            return BadRequest("Failed to exchange Google OAuth code for token.");
        }

        var accessToken = accessTokenProp.GetString();

        Console.WriteLine("✅ Got access_token, fetching Google user info...");
        var userInfo = await http.GetFromJsonAsync<GoogleUserInfo>(
            $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={accessToken}");

        if (userInfo == null)
        {
            Console.WriteLine("❌ Failed to get Google user info");
            return BadRequest("Failed to get Google user info.");
        }

        Console.WriteLine($"✅ Google login successful: {userInfo.Email} ({userInfo.Name})");

        if (UserController.Users.TryGetValue(playerId, out var existingProfile))
        {
            existingProfile.DisplayName = userInfo.Name;
            existingProfile.Email = userInfo.Email;
            existingProfile.AvatarUrl = userInfo.Picture;
            Console.WriteLine($"🔄 Upgraded guest {playerId} → Google account {userInfo.Email}");
        }
        else
        {
            var newProfile = new UserProfile
            {
                PlayerId = playerId,
                DisplayName = userInfo.Name,
                Email = userInfo.Email,
                AvatarUrl = userInfo.Picture
            };
            UserController.Users[playerId] = newProfile;
            Console.WriteLine($"➕ Created new Google user {playerId}");
        }

        Console.WriteLine("✅ Redirecting back to frontend...");
        
        
            
        return Redirect("http://localhost:5170/"); // explicitly redirect to frontend
    }


    public class GoogleUserInfo
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Name { get; set; } = "";
        public string Picture { get; set; } = "";
    }
}

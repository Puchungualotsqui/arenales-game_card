using GameCards.Shared;
using System.Net.Http.Json;

namespace GameCards.Client.Extras;

public class UserProfileService
{
    private readonly LocalStorageService _storage;
    private readonly HttpClient _http;

    public UserProfile? CurrentProfile { get; private set; } = null;
    
    public bool IsLoggedIn => CurrentProfile != null && !string.IsNullOrEmpty(CurrentProfile.PlayerId.ToString());
    
    public async Task RefreshProfileFromServerAsync()
    {
        if (CurrentProfile == null) return;

        try
        {
            var serverProfile = await _http.GetFromJsonAsync<UserProfile>(
                $"api/user/{CurrentProfile.PlayerId}");

            if (serverProfile != null)
            {
                CurrentProfile = serverProfile;
                await _storage.SetItemAsync("userProfile", CurrentProfile);
                Console.WriteLine("✅ Profile refreshed from server");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("❌ Profile not found on backend, registering...");
            // Register this guest automatically
            await _http.PostAsJsonAsync("api/user/register", CurrentProfile);
        }
    }
    
    public async Task<bool> TryLoadProfileAsync()
    {
        var savedProfile = await _storage.GetItemAsync<UserProfile>("userProfile");

        if (savedProfile != null)
        {
            CurrentProfile = savedProfile;
            return true; // already logged (guest or Google)
        }

        return false; // no profile yet
    }

    public UserProfileService(LocalStorageService storage, HttpClient http)
    {
        _storage = storage;
        _http = http;
    }

    public async Task CreateGuessUser()
    {
        var savedProfile = await _storage.GetItemAsync<UserProfile>("userProfile");

        if (savedProfile != null)
        {
            Console.WriteLine("✅ Guest already exists, loading...");
            CurrentProfile = savedProfile;
        }
        else
        {
            // Create a new guest profile
            Console.WriteLine("🆕 Creating new guest profile...");
            CurrentProfile = new UserProfile();

            // Save to local storage
            await _storage.SetItemAsync("userProfile", CurrentProfile);

            // Tell the server about this new guest
            await _http.PostAsJsonAsync("api/user/register", CurrentProfile);
            Console.WriteLine($"✅ Guest created: {CurrentProfile.PlayerId}");
        }
    }

    public async Task SaveAsync()
    {
        await _storage.SetItemAsync("userProfile", CurrentProfile);
    }
    
    public async Task LogoutAsync()
    {
        CurrentProfile = null;
        await _storage.RemoveItemAsync("userProfile");
        Console.WriteLine("✅ Logged out, local profile cleared");
    }
    
    
    private const string PendingRedirectKey = "pendingRedirectUrl";

    public async Task SavePendingRedirectAsync(string url)
    {
        await _storage.SetItemAsync(PendingRedirectKey, url);
    }

    public async Task<string?> GetPendingRedirectAsync()
    {
        return await _storage.GetItemAsync<string>(PendingRedirectKey);
    }

    public async Task ClearPendingRedirectAsync()
    {
        await _storage.RemoveItemAsync(PendingRedirectKey);
    }
}
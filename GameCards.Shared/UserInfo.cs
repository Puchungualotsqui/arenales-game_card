namespace GameCards.Shared;

public class UserProfile
{
    public Guid PlayerId { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = "Guest";
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsGuest => string.IsNullOrEmpty(Email);
    public Guid? CurrentGameId { get; set; }
}
namespace Prm.Data.Entities;

public class RefreshToken
{
    public int RefreshTokenId { get; set; }
    public int UserId { get; set; }
    public required string Token { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public User User { get; set; } = null!;
}

namespace DTO;

public class LoginResponseDTO
{
    public string Token { get; set; } = null!;
    public int AccountId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public List<string> Roles { get; set; } = new();
}


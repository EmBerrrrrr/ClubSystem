using System.Text.Json.Serialization;

namespace DTO;

public class LoginResponseDTO
{
    public string Token { get; set; } = null!;
    public int AccountId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Major { get; set; }
    public string? Skills { get; set; }
    public List<string> Roles { get; set; } = new();
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? PasswordChanged { get; set; } // Chỉ có giá trị khi update profile và có đổi password, không include trong JSON nếu null
}


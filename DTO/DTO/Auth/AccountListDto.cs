namespace DTO.DTO.Admin;

public class AccountListDto
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class ModifyRoleDto
{
    public string RoleName { get; set; } = null!;
}

public class ResetPasswordDto
{
    public string? NewPassword { get; set; }
}
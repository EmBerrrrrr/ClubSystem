using DTO;
using DTO.DTO.Auth;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;

namespace Service.Services;

public interface IAuthBusinessService
{
    Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request);
    Task<LoginResponseDTO?> RegisterAsync(RegisterRequestDTO request);
}

public class AuthBusinessService : IAuthBusinessService
{
    private readonly IAuthRepository _repo;
    private readonly IAuthService _authService;

    public AuthBusinessService(IAuthRepository repo, IAuthService authService)
    {
        _repo = repo;
        _authService = authService;
    }

    public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
    {
        var account = await _repo.GetAccountByUsernameAsync(request.Username);
        if (account == null) return null;

        if (!_authService.VerifyPassword(request.Password, account.PasswordHash))
            return null;

        var roles = await _repo.GetRolesByAccountIdAsync(account.Id);
        var token = _authService.GenerateToken(account, roles);

        return new LoginResponseDTO
        {
            Token = token,
            AccountId = account.Id,
            Username = account.Username,
            Email = account.Email ?? string.Empty,
            FullName = account.FullName ?? string.Empty,
            Roles = roles
        };
    }

    public async Task<LoginResponseDTO?> RegisterAsync(RegisterRequestDTO request)
    {
        // 1. Check username trùng
        var existing = await _repo.GetAccountByUsernameAsync(request.Username);
        if (existing != null) return null;

        // 2. Tạo account mới
        var account = new Account
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _authService.HashPassword(request.Password),
            FullName = request.FullName,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _repo.AddAccountAsync(account);

        // 3. Gán role mặc định "User" trong bảng roles + account_roles
        const string defaultRoleName = "Student";

        // lấy roleId theo tên
        var roleId = await _repo.GetRoleIdByNameAsync(defaultRoleName);

        // thêm vào bảng account_roles
        await _repo.AddAccountRoleAsync(account.Id, roleId);

        var roles = new List<string> { defaultRoleName };

        // 4. Tạo token
        var token = _authService.GenerateToken(account, roles);

        // 5. Trả response
        return new LoginResponseDTO
        {
            Token = token,
            AccountId = account.Id,
            Username = account.Username,
            Email = account.Email ?? string.Empty,
            FullName = account.FullName ?? string.Empty,
            Roles = roles
        };
    }
}

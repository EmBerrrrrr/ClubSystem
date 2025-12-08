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
        var existing = await _repo.GetAccountByUsernameAsync(request.Username);
        if (existing != null) return null;

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

        var roles = new List<string> { "User" };
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

}



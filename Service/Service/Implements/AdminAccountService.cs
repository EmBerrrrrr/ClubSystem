using DTO.DTO.Admin;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Service.Service.Implements;

public class AdminAccountService : IAdminAccountService
{
    private readonly IAuthRepository _repo;
    private readonly IAuthService _authService;

    private static readonly HashSet<string> PermittedRoles = new()
    {
        "student","clubleader","admin"
    };

    public AdminAccountService(IAuthRepository repo, IAuthService authService)
    {
        _repo = repo;
        _authService = authService;
    }

    public async Task<List<AccountListDto>> GetAllAccounts()
    {
        var list = await _repo.GetAllAccountsAsync();

        var result = new List<AccountListDto>();

        foreach (var acc in list)
        {
            result.Add(await Map(acc));
        }

        return result;
    }

    public async Task<AccountListDto?> GetAccount(int id)
    {
        var acc = await _repo.GetAccountByIdAsync(id);

        if (acc == null) return null;

        return await Map(acc);
    }

    public async Task LockAccount(int id)
    {
        var acc = await _repo.GetAccountByIdAsync(id)
                  ?? throw new Exception("Account not found");

        acc.IsActive = false;
        await _repo.UpdateAccountAsync(acc);
    }

    public async Task ActivateAccount(int id)
    {
        var acc = await _repo.GetAccountByIdAsync(id)
                  ?? throw new Exception("Account not found");

        acc.IsActive = true;
        await _repo.UpdateAccountAsync(acc);
    }

    public async Task<string> ResetPassword(int id, string? newPassword)
    {
        var acc = await _repo.GetAccountByIdAsync(id)
                  ?? throw new Exception("Account not found");

        var pwd = string.IsNullOrWhiteSpace(newPassword)
            ? GeneratePassword()
            : newPassword;

        acc.PasswordHash = _authService.HashPassword(pwd);

        await _repo.UpdateAccountAsync(acc);

        return pwd;
    }

    public async Task AddRole(int id, string role)
    {
        ValidateRole(role);

        var roleId = await _repo.GetRoleIdByNameAsync(role);
        await _repo.AddAccountRoleAsync(id, roleId);
    }

    public async Task RemoveRole(int id, string role)
    {
        ValidateRole(role);

        var roleId = await _repo.GetRoleIdByNameAsync(role);
        await _repo.RemoveAccountRoleAsync(id, roleId);
    }

    #region helpers

    private async Task<AccountListDto> Map(Account acc)
    {
        var roles = await _repo.GetRolesByAccountIdAsync(acc.Id);

        return new AccountListDto
        {
            Id = acc.Id,
            Username = acc.Username,
            Email = acc.Email,
            FullName = acc.FullName,
            IsActive = acc.IsActive ?? false,
            Roles = roles
        };
    }

    private static string GeneratePassword(int length = 10)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@!$";

        using var rng = RandomNumberGenerator.Create();
        var data = new byte[length];
        rng.GetBytes(data);

        var sb = new StringBuilder(length);

        foreach (var b in data)
            sb.Append(chars[b % chars.Length]);

        return sb.ToString();
    }

    private static void ValidateRole(string role)
    {
        if (!PermittedRoles.Contains(role.Trim().ToLower()))
            throw new Exception("Invalid role.");
    }

    #endregion
}
using DTO.DTO.Admin;

namespace Service.Service.Interfaces;

public interface IAdminAccountService
{
    Task<List<AccountListDto>> GetAllAccounts();
    Task<AccountListDto?> GetAccount(int id);

    Task LockAccount(int id);
    Task ActivateAccount(int id);

    Task<string> ResetPassword(int id, string? newPassword);

    Task AddRole(int id, string role);
    Task RemoveRole(int id, string role);
    Task<int> HashAllPasswordsAsync();
}

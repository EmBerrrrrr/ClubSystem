using Repository.Models;

namespace Repository.Repo.Interfaces;

public interface IAuthRepository
{
    Task<Account?> GetAccountByUsernameAsync(string username);
    Task<List<string>> GetRolesByAccountIdAsync(int accountId);
    Task AddAccountAsync(Account account);
    Task<int> GetRoleIdByNameAsync(string roleName);
    Task AddAccountRoleAsync(int accountId, int roleId);
}

using Repository.Models;

namespace Repository.Repo.Interfaces;

public interface IAuthRepository
{
    Task<Account?> GetAccountByUsernameAsync(string username);
    Task<List<string>> GetRolesByAccountIdAsync(int accountId);
    Task AddAccountAsync(Account account);
}

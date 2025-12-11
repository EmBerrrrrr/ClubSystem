using Repository.Models;

namespace Repository.Repo.Interfaces;

public interface IAuthRepository
{
    Task<Account?> GetAccountByUsernameAsync(string username);
    Task<Account?> GetAccountByEmailAsync(string email);
    Task<List<string>> GetRolesByAccountIdAsync(int accountId);
    Task AddAccountAsync(Account account);
    Task<int> GetRoleIdByNameAsync(string roleName);
    Task AddAccountRoleAsync(int accountId, int roleId);
    Task<List<Account>> GetAllAccountsAsync();        
    Task<Account?> GetAccountByIdAsync(int id);         
    Task UpdateAccountAsync(Account account);             
    Task RemoveAccountRoleAsync(int accountId, int roleId);   
}

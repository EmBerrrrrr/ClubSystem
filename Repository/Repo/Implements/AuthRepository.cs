using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements;

public class AuthRepository : IAuthRepository
{
    private readonly StudentClubManagementContext _context;

    public AuthRepository(StudentClubManagementContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetAccountByUsernameAsync(string username)
        => await _context.Accounts
            .SingleOrDefaultAsync(a => a.Username == username);

    public async Task<List<string>> GetRolesByAccountIdAsync(int accountId)
    {
        return await _context.AccountRoles
            .Where(ar => ar.AccountId == accountId)
            .Select(ar => ar.Role.Name)
            .ToListAsync();
    }

    public async Task AddAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetRoleIdByNameAsync(string roleName)
    {
        var role = await _context.Roles
            .SingleAsync(r => r.Name == roleName);
        return role.Id;
    }

    public async Task AddAccountRoleAsync(int accountId, int roleId)
    {
        var ar = new AccountRole
        {
            AccountId = accountId,
            RoleId = roleId
        };
        _context.AccountRoles.Add(ar);
        await _context.SaveChangesAsync();
    }
    public async Task<List<Account>> GetAllAccountsAsync()     
    {
        return await _context.Accounts
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Account?> GetAccountByIdAsync(int id)    
    {
        return await _context.Accounts
            .SingleOrDefaultAsync(a => a.Id == id);
    }

    public async Task UpdateAccountAsync(Account account)  
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAccountRoleAsync(int accountId, int roleId) 
    {
        var ar = await _context.AccountRoles
            .SingleOrDefaultAsync(x => x.AccountId == accountId && x.RoleId == roleId);

        if (ar != null)
        {
            _context.AccountRoles.Remove(ar);
            await _context.SaveChangesAsync();
        }
    }
}

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
            .FirstOrDefaultAsync(a => a.Username == username);

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

}

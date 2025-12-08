using Repository.Models;

namespace Service.Service.Interfaces
{
    public interface IAuthService
    {
        string GenerateToken(Account account, List<string> roles);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }
}

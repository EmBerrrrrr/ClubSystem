using DTO;
using Repository.Models;

namespace Repository.Repo.Interfaces
{
    public interface IClubRepository
    {
        Task<Club?> GetByIdAsync(int id);
        Task<List<Club>> GetAllAsync();
        Task<List<Club>> GetByLeaderIdAsync(int leaderId);
        Task<Club?> GetDetailWithActivitiesAsync(int id);
        Task AddAsync(Club club);
        Task UpdateAsync(Club club);
        Task DeleteAsync(Club club);
        Task<bool> IsLeaderOfClubAsync(int clubId, int accountId);
        Task<List<int>> GetLeaderAccountIdsByClubIdAsync(int clubId);
    }

}

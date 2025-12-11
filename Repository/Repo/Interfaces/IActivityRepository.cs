using Repository.Models;  

namespace Repository.Repo.Interfaces
{
    public interface IActivityRepository
    {
        Task<List<Activity>> GetAllAsync();
        Task<List<Activity>> GetByClubAsync(int clubId);
        Task<Activity?> GetByIdAsync(int id);
        Task AddAsync(Activity activity);
        Task UpdateAsync(Activity activity);
        Task DeleteAsync(Activity activity);
        Task<bool> IsLeaderOfClubAsync(int clubId, int accountId);
    }

}

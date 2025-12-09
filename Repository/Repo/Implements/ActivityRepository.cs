using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly StudentClubManagementContext _context;

        public ActivityRepository(StudentClubManagementContext context)
        {
            _context = context;
        }
        public Task<List<Activity>> GetAllAsync()
            => _context.Activities.OrderByDescending(x => x.StartTime).ToListAsync();
        public Task<List<Activity>> GetByClubAsync(int clubId)
            => _context.Activities
                .Where(x => x.ClubId == clubId)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();
        public Task<Activity?> GetByIdAsync(int id)
            => _context.Activities.FirstOrDefaultAsync(x => x.Id == id);
        public async Task AddAsync(Activity activity)
        {
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Activity activity)
        {
            _context.Activities.Update(activity);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(Activity activity)
        {
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();
        }
        public Task<bool> IsLeaderOfClubAsync(int clubId, int accountId)
            => _context.ClubLeaders.AnyAsync(x =>
                x.ClubId == clubId &&
                x.AccountId == accountId &&
                x.IsActive == true);
    }

}
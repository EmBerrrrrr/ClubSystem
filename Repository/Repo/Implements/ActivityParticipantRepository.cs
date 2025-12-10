using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    public class ActivityParticipantRepository : IActivityParticipantRepository
    {
        private readonly StudentClubManagementContext _context;

        public ActivityParticipantRepository(StudentClubManagementContext context)
        {
            _context = context;
        }

        public async Task<bool> IsRegisteredAsync(int activityId, int membershipId)
        {
            return await _context.ActivityParticipants
                .AnyAsync(ap => ap.ActivityId == activityId && ap.MembershipId == membershipId);
        }

        public async Task<ActivityParticipant?> GetByIdAsync(int id)
        {
            return await _context.ActivityParticipants
                .Include(ap => ap.Activity)
                .Include(ap => ap.Membership)
                .ThenInclude(m => m.Club)
                .FirstOrDefaultAsync(ap => ap.Id == id);
        }

        public async Task<List<ActivityParticipant>> GetByMembershipIdAsync(int membershipId)
        {
            return await _context.ActivityParticipants
                .Include(ap => ap.Activity)
                .ThenInclude(a => a.Club)
                .Where(ap => ap.MembershipId == membershipId)
                .OrderByDescending(ap => ap.RegisterTime)
                .ToListAsync();
        }

        public async Task<List<ActivityParticipant>> GetByActivityIdAsync(int activityId)
        {
            return await _context.ActivityParticipants
                .Include(ap => ap.Membership)
                .ThenInclude(m => m.Account)
                .Where(ap => ap.ActivityId == activityId)
                .ToListAsync();
        }

        public async Task AddParticipantAsync(ActivityParticipant participant)
        {
            await _context.ActivityParticipants.AddAsync(participant);
        }

        public async Task UpdateParticipantAsync(ActivityParticipant participant)
        {
            _context.ActivityParticipants.Update(participant);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}


using Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Repo.Interfaces
{
    public interface IActivityParticipantRepository
    {
        Task<bool> IsRegisteredAsync(int activityId, int membershipId);
        Task<ActivityParticipant?> GetByIdAsync(int id);
        Task<List<ActivityParticipant>> GetByMembershipIdAsync(int membershipId);
        Task<List<ActivityParticipant>> GetByActivityIdAsync(int activityId);
        Task AddParticipantAsync(ActivityParticipant participant);
        Task UpdateParticipantAsync(ActivityParticipant participant);
        Task SaveAsync();
    }
}


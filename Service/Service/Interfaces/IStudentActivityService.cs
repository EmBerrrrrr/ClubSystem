using DTO.DTO.Activity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IStudentActivityService
    {
        Task RegisterForActivityAsync(int accountId, int activityId);
        Task CancelRegistrationAsync(int accountId, int activityId);
        Task<List<ActivityParticipantDto>> GetMyActivityHistoryAsync(int accountId);
        Task<List<ActivityDto>> GetAvailableActivitiesForMyClubsAsync(int accountId);
    }
}


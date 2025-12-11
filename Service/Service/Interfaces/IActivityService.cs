using DTO.DTO.Activity;

namespace Service.Services
{
    public interface IActivityService
    {
        Task<List<ActivityDto>> GetAllAsync();
        Task<List<ActivityDto>> GetByClubAsync(int clubId);
        Task<ActivityDto?> GetDetailAsync(int id);
        Task<ActivityDto> CreateAsync(CreateActivityDto dto, int accountId, bool isAdmin);
        Task UpdateAsync(int id, UpdateActivityDto dto, int accountId, bool isAdmin);
        Task DeleteAsync(int id, int accountId, bool isAdmin);
        Task OpenRegistrationAsync(int activityId, int leaderId);
        Task CloseRegistrationAsync(int activityId, int leaderId);
        Task<List<ActivityParticipantForLeaderDto>> GetActivityParticipantsAsync(int activityId, int leaderId);
    }
}
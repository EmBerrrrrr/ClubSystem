using DTO.DTO.Activity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IStudentActivityService
    {
        Task RegisterForActivityAsync(int accountId, int activityId);
        Task CancelRegistrationAsync(int accountId, int activityId, string? reason);
        Task<List<ActivityParticipantDto>> GetMyActivityHistoryAsync(int accountId);
        Task<List<ActivityDto>> GetActivitiesForRegistrationAsync(int accountId); // Chỉ cho member đăng ký
        Task<List<ActivityDto>> GetAllActivitiesForViewingAsync(); // Student xem tất cả activities (không cần là member)
        Task<List<ActivityDto>> GetActivitiesByClubForViewingAsync(int clubId); // Student xem activities của một CLB cụ thể
    }
}


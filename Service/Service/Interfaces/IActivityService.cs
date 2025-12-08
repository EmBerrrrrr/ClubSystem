using DTO;
using Repository.Repo.Interfaces;

namespace Service.Services;

public interface IActivityService
{
    Task<List<ActivityDTO>> GetActivitiesByClubIdAsync(int clubId);
    Task<ActivityDTO?> GetActivityByIdAsync(int id);
}
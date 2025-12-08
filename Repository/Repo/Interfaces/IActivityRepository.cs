using DTO;
using Repository.Models;  

namespace Repository.Repo.Interfaces;

public interface IActivityRepository
{
    Task<List<ActivityDTO>> GetActivitiesByClubIdAsync(int clubId);
    Task<ActivityDTO?> GetActivityByIdAsync(int id);
}

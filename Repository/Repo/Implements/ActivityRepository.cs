using Microsoft.EntityFrameworkCore;
using DTO;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements;

public class ActivityRepository : IActivityRepository
{
    private readonly StudentClubManagementContext _db;

    public ActivityRepository(StudentClubManagementContext db)
    {
        _db = db;
    }

    public async Task<List<ActivityDTO>> GetActivitiesByClubIdAsync(int clubId)
    {
        return await _db.Activities
            .Where(a => a.ClubId == clubId)
            .OrderByDescending(a => a.StartTime)
            .Select(a => new ActivityDTO
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title,
                Description = a.Description,
                StartTime = (DateTime)a.StartTime,
                EndTime = (DateTime)a.EndTime,
                Location = a.Location,
                Status = a.Status,
                CreatedBy = a.CreatedBy,
                ApprovedBy = a.ApprovedBy
            })
            .ToListAsync();
    }

    public async Task<ActivityDTO?> GetActivityByIdAsync(int id)
    {
        return await _db.Activities
            .Where(a => a.Id == id)
            .Select(a => new ActivityDTO
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title,
                Description = a.Description,
                StartTime = (DateTime)a.StartTime,
                EndTime = (DateTime)a.EndTime,
                Location = a.Location,
                Status = a.Status,
                CreatedBy = a.CreatedBy,
                ApprovedBy = a.ApprovedBy
            })
            .FirstOrDefaultAsync();
    }
}

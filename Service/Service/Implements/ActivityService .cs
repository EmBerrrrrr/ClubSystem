using DTO;
using Repository.Repo.Interfaces;
using Service.Services;
using System;


public class ActivityService : IActivityService
{
    private readonly IActivityRepository _repo;

    public ActivityService(IActivityRepository repo)
    {
        _repo = repo;
    }

    public Task<List<ActivityDTO>> GetActivitiesByClubIdAsync(int clubId)
        => _repo.GetActivitiesByClubIdAsync(clubId);

    public Task<ActivityDTO?> GetActivityByIdAsync(int id)
        => _repo.GetActivityByIdAsync(id);
}

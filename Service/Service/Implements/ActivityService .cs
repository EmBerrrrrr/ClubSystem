using DTO.DTO.Activity;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Services;
using System;

namespace Service.Service.Implements
{
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _repo;

        public ActivityService(IActivityRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ActivityDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(Map).ToList();
        }

        public async Task<List<ActivityDto>> GetByClubAsync(int clubId)
        {
            var list = await _repo.GetByClubAsync(clubId);
            return list.Select(Map).ToList();
        }

        public async Task<ActivityDto?> GetDetailAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<ActivityDto> CreateAsync(CreateActivityDto dto, int accountId, bool isAdmin)
        {
            if (!isAdmin)
            {
                var isLeader = await _repo.IsLeaderOfClubAsync(dto.ClubId, accountId);
                if (!isLeader)
                    throw new UnauthorizedAccessException("Bạn không phải leader CLB này");
            }

            var entity = new Activity
            {
                ClubId = dto.ClubId,
                Title = dto.Title,
                Description = dto.Description,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Location = dto.Location,
                Status = "Pending",
                CreatedBy = accountId
            };

            await _repo.AddAsync(entity);

            return Map(entity);
        }

        public async Task UpdateAsync(int id, UpdateActivityDto dto, int accountId, bool isAdmin)
        {
            var entity = await _repo.GetByIdAsync(id)
                ?? throw new Exception("Activity not found");

            if (!isAdmin)
            {
                var isLeader = await _repo.IsLeaderOfClubAsync(entity.ClubId, accountId);
                if (!isLeader)
                    throw new UnauthorizedAccessException("Bạn không phải leader CLB này");
            }

            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.StartTime = dto.StartTime;
            entity.EndTime = dto.EndTime;
            entity.Location = dto.Location;
            entity.Status = dto.Status ?? entity.Status;

            await _repo.UpdateAsync(entity);
        }

        public async Task DeleteAsync(int id, int accountId, bool isAdmin)
        {
            var entity = await _repo.GetByIdAsync(id)
                ?? throw new Exception("Activity not found");

            if (!isAdmin)
            {
                var isLeader = await _repo.IsLeaderOfClubAsync(entity.ClubId, accountId);
                if (!isLeader)
                    throw new UnauthorizedAccessException("Bạn không phải leader CLB này");
            }

            await _repo.DeleteAsync(entity);
        }

        private static ActivityDto Map(Activity a)
        {
            return new ActivityDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title,
                Description = a.Description,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Location = a.Location,
                Status = a.Status,
                CreatedBy = a.CreatedBy
            };
        }
    }
}

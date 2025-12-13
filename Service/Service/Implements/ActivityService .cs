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
        private readonly IActivityParticipantRepository _participantRepo;

        public ActivityService(IActivityRepository repo, IActivityParticipantRepository participantRepo)
        {
            _repo = repo;
            _participantRepo = participantRepo;
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
                // Club leader tạo activity mặc định với status "Not_yet_open" (chưa mở đăng ký)
                // Có thể dùng OpenRegistrationAsync để mở đăng ký sau
                Status = "Not_yet_open",
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

            // XÓA PARTICIPANTS TRƯỚC
            await _participantRepo.DeleteByActivityIdAsync(id);

            // SAU ĐÓ XÓA ACTIVITY
            await _repo.DeleteAsync(entity);
        }


        // Mở đăng ký activity (chuyển từ "Not_yet_open" hoặc "Active_Closed" sang "Active")
        public async Task OpenRegistrationAsync(int activityId, int leaderId)
        {
            var activity = await _repo.GetByIdAsync(activityId)
                ?? throw new Exception("Không tìm thấy activity.");

            // Kiểm tra leader có quyền với CLB này không
            if (!await _repo.IsLeaderOfClubAsync(activity.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (activity.Status == "Active")
                throw new Exception("Đăng ký đã được mở.");

            if (activity.Status == "Cancelled" || activity.Status == "Completed")
                throw new Exception("Không thể mở đăng ký cho activity đã bị hủy hoặc đã hoàn thành.");

            activity.Status = "Active";
            await _repo.UpdateAsync(activity);
        }

        // Đóng đăng ký activity (status = "Active_Closed")
        public async Task CloseRegistrationAsync(int activityId, int leaderId)
        {
            var activity = await _repo.GetByIdAsync(activityId)
                ?? throw new Exception("Không tìm thấy activity.");

            // Kiểm tra leader có quyền với CLB này không
            if (!await _repo.IsLeaderOfClubAsync(activity.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (activity.Status == "Active_Closed")
                throw new Exception("Đăng ký đã được đóng.");

            activity.Status = "Active_Closed";
            await _repo.UpdateAsync(activity);
        }

        // Xem danh sách participants của activity
        public async Task<List<ActivityParticipantForLeaderDto>> GetActivityParticipantsAsync(int activityId, int leaderId)
        {
            var activity = await _repo.GetByIdAsync(activityId)
                ?? throw new Exception("Không tìm thấy activity.");

            // Kiểm tra leader có quyền với CLB này không
            if (!await _repo.IsLeaderOfClubAsync(activity.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            var participants = await _participantRepo.GetByActivityIdAsync(activityId);

            return participants.Select(p => new ActivityParticipantForLeaderDto
            {
                ParticipantId = p.Id,
                MembershipId = p.MembershipId,
                AccountId = p.Membership?.AccountId ?? 0,
                FullName = p.Membership?.Account?.FullName ?? "",
                Email = p.Membership?.Account?.Email ?? "",
                Phone = p.Membership?.Account?.Phone ?? "",
                RegisterTime = p.RegisterTime,
                Attended = p.Attended,
                //CancelReason = p.CancelReason
            }).ToList();
        }

        private static ActivityDto Map(Activity a)
        {
            // Tính status động dựa trên thời gian hiện tại
            var calculatedStatus = CalculateActivityStatus(a);

            return new ActivityDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title,
                Description = a.Description,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Location = a.Location,
                Status = calculatedStatus, // Sử dụng status đã tính toán
                CreatedBy = a.CreatedBy,
                ImageActsUrl = a.ImageActsUrl,
                AvatarPublicId = a.AvatarPublicId
            };
        }

        // Tính status của activity dựa trên thời gian hiện tại
        private static string CalculateActivityStatus(Activity a)
        {
            var now = DateTime.Now;
            
            // Nếu status là Cancelled hoặc Completed, giữ nguyên
            if (a.Status == "Cancelled" || a.Status == "Completed")
                return a.Status;

            // Kiểm tra nếu activity đang diễn ra
            if (a.StartTime.HasValue && a.EndTime.HasValue)
            {
                if (now >= a.StartTime.Value && now <= a.EndTime.Value)
                {
                    return "Ongoing"; // Đang diễn ra
                }
            }
            else if (a.StartTime.HasValue && !a.EndTime.HasValue)
            {
                // Chỉ có start_time, nếu đã bắt đầu thì là Ongoing
                if (now >= a.StartTime.Value)
                {
                    return "Ongoing";
                }
            }

            // Nếu đã kết thúc (có end_time và đã qua end_time)
            if (a.EndTime.HasValue && now > a.EndTime.Value)
            {
                // Chỉ tự động set Completed nếu status hiện tại là Active hoặc Active_Closed
                if (a.Status == "Active" || a.Status == "Active_Closed" || a.Status == "Ongoing")
                {
                    return "Completed";
                }
            }

            // Trả về status gốc (Not_yet_open, Active, Active_Closed)
            return a.Status;
        }
    }
}

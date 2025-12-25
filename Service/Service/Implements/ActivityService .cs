using DTO.DTO.Activity;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Services;
using System;
using Service.Helper; 

namespace Service.Service.Implements
{
    /// <summary>
    /// Service xử lý các thao tác liên quan đến Activity dành cho Club Leader và Admin.
    /// 
    /// Chức năng chính:
    /// - Tạo, sửa, xóa activity
    /// - Mở/đóng đăng ký tham gia
    /// - Xem danh sách người tham gia activity
    /// 
    /// Quyền truy cập:
    /// - Chỉ Club Leader của CLB tổ chức hoặc Admin mới được thực hiện các thao tác.
    /// - Nếu Club bị "Locked" → không cho phép bất kỳ thao tác nào liên quan đến activity của club đó.
    /// 
    /// Tương tác với Membership:
    /// - Activity chỉ cho phép member active đăng ký (xử lý ở StudentActivityService).
    /// - Leader không cần kiểm tra membership vì họ quản lý CLB.
    /// </summary>
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _repo;
        private readonly IActivityParticipantRepository _participantRepo;
        private readonly IClubRepository _clubRepo;  

        public ActivityService(IActivityRepository repo, IActivityParticipantRepository participantRepo, IClubRepository clubRepo)
        {
            _repo = repo;
            _participantRepo = participantRepo;
            _clubRepo = clubRepo;
        }

        /// <summary>
        /// Lấy tất cả activity (dùng cho admin hoặc hiển thị public).
        /// 
        /// API: GET /api/activities
        /// Luồng: Lấy từ DB → tính status động → trả về DTO.
        /// </summary>
        public async Task<List<ActivityDto>> GetAllAsync()
        {
            var list = await _repo.GetAllAsync();
            return list.Select(Map).ToList();
        }

        /// <summary>
        /// Lấy activity theo clubId.
        /// 
        /// API: GET /api/activities/club/{clubId}
        /// </summary>
        public async Task<List<ActivityDto>> GetByClubAsync(int clubId)
        {
            var list = await _repo.GetByClubAsync(clubId);
            return list.Select(Map).ToList();
        }

        /// <summary>
        /// Lấy chi tiết một activity.
        /// 
        /// API: GET /api/activities/{id}
        /// </summary>
        public async Task<ActivityDto?> GetDetailAsync(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : Map(entity);
        }

        /// <summary>
        /// Tạo activity mới.
        /// 
        /// API: POST /api/activities
        /// Luồng:
        /// - Front-end gửi CreateActivityDto → Leader gọi API
        /// - Kiểm tra club tồn tại và không bị locked
        /// - Kiểm tra quyền: phải là leader của club hoặc admin
        /// - Tạo entity Activity với Status = "Not_yet_open"
        /// - Lưu vào bảng Activity trong DB
        /// </summary>
        public async Task<ActivityDto> CreateAsync(CreateActivityDto dto, int accountId, bool isAdmin)
        {
            var club = await _clubRepo.GetByIdAsync(dto.ClubId);  
            if (club == null) throw new Exception("Club not found");
            if (club.Status != null && club.Status.ToLower() == "locked") throw new Exception("Cannot create activity for locked club");  

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
                Status = "Not_yet_open",
                CreatedBy = accountId
            };

            await _repo.AddAsync(entity);

            return Map(entity);
        }

        /// <summary>
        /// Cập nhật activity.
        /// 
        /// API: PUT /api/activities/{id}
        /// Luồng: Cập nhật các field, có thể thay đổi Status (leader tự quản lý).
        /// </summary>
        public async Task UpdateAsync(int id, UpdateActivityDto dto, int accountId, bool isAdmin)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new Exception("Activity not found");

            var club = await _clubRepo.GetByIdAsync(entity.ClubId) 
                ?? throw new Exception("Club not found");
            if (club.Status == "Locked") throw new Exception("Cannot update activity for locked club");  

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

        /// <summary>
        /// Xóa activity (chỉ khi đã Cancelled hoặc Completed).
        /// 
        /// API: DELETE /api/activities/{id}
        /// Luồng: Xóa tất cả participant trước → xóa activity.
        /// </summary>
        public async Task DeleteAsync(int id, int accountId, bool isAdmin)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) throw new Exception("Activity not found");

            var club = await _clubRepo.GetByIdAsync(entity.ClubId) 
                ?? throw new Exception("Club not found");
            if (club.Status == "Locked") throw new Exception("Cannot delete activity for locked club"); 

            if (!isAdmin)
            {
                var isLeader = await _repo.IsLeaderOfClubAsync(entity.ClubId, accountId);
                if (!isLeader)
                    throw new UnauthorizedAccessException("Bạn không phải leader CLB này");
            }

            var currentStatus = entity.Status?.Trim() ?? "";
            if (currentStatus != "Cancelled" && currentStatus != "Completed")
            {
                throw new Exception("Không thể xóa hoạt động. Vui lòng dừng hoạt động (hủy hoặc đánh dấu hoàn thành) trước khi xóa.");
            }

            await _participantRepo.DeleteByActivityIdAsync(id);
            await _repo.DeleteAsync(entity);
        }

        /// <summary>
        /// Mở đăng ký tham gia activity.
        /// 
        /// API: PUT /api/activities/{id}/open-registration
        /// Luồng: Chỉ leader mới được gọi → đổi Status thành "Active".
        /// </summary>
        public async Task OpenRegistrationAsync(int activityId, int leaderId)
        {
            var activity = await _repo.GetByIdAsync(activityId);
            if (activity == null) throw new Exception("Activity not found");

            var club = await _clubRepo.GetByIdAsync(activity.ClubId) 
                ?? throw new Exception("Club not found");
            if (club.Status == "Locked") throw new Exception("Cannot open registration for activity in locked club");  

            if (!await _repo.IsLeaderOfClubAsync(activity.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (activity.Status != null && activity.Status.ToLower() == "active")
                throw new Exception("Đăng ký đã được mở.");

            if (activity.Status != null && (activity.Status.ToLower() == "cancelled" || activity.Status.ToLower() == "completed"))
                throw new Exception("Không thể mở đăng ký cho activity đã bị hủy hoặc đã hoàn thành.");

            activity.Status = "Active";
            await _repo.UpdateAsync(activity);
        }

        /// <summary>
        /// Đóng đăng ký tham gia activity.
        /// 
        /// API: PUT /api/activities/{id}/close-registration
        /// </summary>
        public async Task CloseRegistrationAsync(int activityId, int leaderId)
        {
            var activity = await _repo.GetByIdAsync(activityId);
            if (activity == null) throw new Exception("Activity not found");

            var club = await _clubRepo.GetByIdAsync(activity.ClubId) 
                ?? throw new Exception("Club not found");
            if (club.Status == "Locked") throw new Exception("Cannot close registration for activity in locked club"); 

            if (!await _repo.IsLeaderOfClubAsync(activity.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (activity.Status != null && activity.Status.ToLower() == "active_closed")
                throw new Exception("Đăng ký đã được đóng.");

            activity.Status = "Active_Closed";
            await _repo.UpdateAsync(activity);
        }

        /// <summary>
        /// Leader xem danh sách người tham gia activity.
        /// 
        /// API: GET /api/activities/{id}/participants
        /// Luồng: Lấy từ ActivityParticipant → join Membership → Account để lấy thông tin cá nhân.
        /// </summary>
        public async Task<List<ActivityParticipantForLeaderDto>> GetActivityParticipantsAsync(int activityId, int leaderId)
        {
            var activity = await _repo.GetByIdAsync(activityId)
                ?? throw new Exception("Không tìm thấy activity.");

            if (!await _repo.IsLeaderOfClubAsync(activity.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            var participants = await _participantRepo.GetByActivityIdAsync(activityId);

            return participants.Select(p => new ActivityParticipantForLeaderDto
            {
                ParticipantId = p.Id,
                MembershipId = p.MembershipId ?? 0,
                AccountId = p.Membership?.AccountId ?? 0,
                FullName = p.Membership?.Account?.FullName ?? "",
                Email = p.Membership?.Account?.Email ?? "",
                Phone = p.Membership?.Account?.Phone ?? "",
                RegisterTime = p.RegisterTime.ToVietnamTime(),
                Attended = p.Attended,
            }).ToList();
        }

        private static ActivityDto Map(Activity a)
        {
            var calculatedStatus = CalculateActivityStatus(a);

            return new ActivityDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title,
                Description = a.Description ?? string.Empty,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Location = a.Location ?? string.Empty,
                Status = calculatedStatus,
                CreatedBy = a.CreatedBy,
                ImageActsUrl = a.ImageActsUrl,
                AvatarPublicId = a.AvatarPublicId
            };
        }

        private static string CalculateActivityStatus(Activity a)
        {
            var now = DateTimeExtensions.NowVietnam();

            if (a.Status != null && (a.Status.ToLower() == "cancelled" || a.Status.ToLower() == "completed"))
                return a.Status ?? string.Empty;

            if (a.StartTime.HasValue && a.EndTime.HasValue)
            {
                if (now >= a.StartTime.Value && now <= a.EndTime.Value)
                    return "Ongoing";
            }
            else if (a.StartTime.HasValue && !a.EndTime.HasValue)
            {
                if (now >= a.StartTime.Value)
                    return "Ongoing";
            }

            if (a.EndTime.HasValue && now > a.EndTime.Value)
            {
                if (a.Status != null && (a.Status.ToLower() == "active" || a.Status.ToLower() == "active_closed" || a.Status.ToLower() == "ongoing"))
                    return "Completed";
            }

            return a.Status ?? string.Empty;
        }
    }
}

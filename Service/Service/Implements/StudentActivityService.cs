using DTO.DTO.Activity;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using Service.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Service xử lý các thao tác liên quan đến Activity dành cho sinh viên/student.
/// 
/// Chức năng chính:
/// - Đăng ký / hủy đăng ký tham gia activity (chỉ dành cho thành viên active của CLB)
/// - Xem lịch sử tham gia activity của bản thân
/// - Xem danh sách activity để đăng ký (chỉ những activity đang mở đăng ký và thuộc CLB mà mình là member)
/// - Xem tất cả activity đang mở để xem thông tin (không cần là member)
/// 
/// Tương tác quan trọng với Membership:
/// - Chỉ thành viên có Membership.Status = "active" mới được gọi RegisterForActivityAsync.
/// - Nếu chưa là member → hướng dẫn gửi MembershipRequest trước.
/// - Nếu club bị "Locked" → không cho đăng ký activity của club đó.
/// </summary>
namespace Service.Service.Implements
{
    public class StudentActivityService : IStudentActivityService
    {
        private readonly IActivityParticipantRepository _participantRepo;
        private readonly IActivityRepository _activityRepo;
        private readonly IMembershipRepository _membershipRepo;
        private readonly IMembershipRequestRepository _membershipRequestRepo;
        private readonly IClubRepository _clubRepo;  // THÊM: Để check club status

        public StudentActivityService(
            IActivityParticipantRepository participantRepo,
            IActivityRepository activityRepo,
            IMembershipRepository membershipRepo,
            IMembershipRequestRepository membershipRequestRepo,
            IClubRepository clubRepo)
        {
            _participantRepo = participantRepo;
            _activityRepo = activityRepo;
            _membershipRepo = membershipRepo;
            _membershipRequestRepo = membershipRequestRepo;
            _clubRepo = clubRepo;
        }

        /// <summary>
        /// Sinh viên (là thành viên active) đăng ký tham gia một activity.
        /// 
        /// API: POST /api/student/activities/{activityId}/register
        /// Luồng:
        /// - Front-end gọi → Controller → Method này
        /// - Kiểm tra: activity tồn tại, club không bị locked, trạng thái activity cho phép đăng ký
        /// - Kiểm tra người dùng có Membership active của CLB tổ chức activity không
        /// - Nếu hợp lệ → tạo bản ghi ActivityParticipant (Attended = true, RegisterTime = now)
        /// - Lưu vào bảng ActivityParticipant trong DB
        /// </summary>
        // Member đăng ký tham gia activity (không phải student, student phải trở thành member trước)
        public async Task RegisterForActivityAsync(int accountId, int activityId)
        {
            // 1. Kiểm tra activity tồn tại
            var activity = await _activityRepo.GetByIdAsync(activityId);
            if (activity == null) throw new Exception("Hoạt động không tồn tại");

            var club = await _clubRepo.GetByIdAsync(activity.ClubId);  // THÊM: Get club từ activity
            if (club == null) throw new Exception("Câu lạc bộ không tồn tại");
            if (club.Status == "Locked") throw new Exception("Bạn không thể đăng kí vào câu lạc bộ đã bị khóa.");  // THÊM

            // 2. Tính status thực tế dựa trên thời gian
            var actualStatus = CalculateActivityStatus(activity);

            // 3. Kiểm tra activity không bị hủy, đã hoàn thành, hoặc đang diễn ra
            if (actualStatus == "Cancelled")
                throw new Exception("Hoạt động này đã bị hủy.");
            
            if (actualStatus == "Completed")
                throw new Exception("Hoạt động này đã hoàn thành.");

            if (actualStatus == "Ongoing")
                throw new Exception("Hoạt động này đang diễn ra, không thể đăng ký thêm.");

            if (activity.Status == "Active_Closed" || activity.Status == "closed")
                throw new Exception("Đăng ký cho hoạt động này đã được đóng.");

            if (activity.Status == "Not_yet_open" || activity.Status == "draft")
                throw new Exception("Hoạt động này chưa mở đăng ký.");

            if (activity.Status != "Active" && activity.Status != "opened")
                throw new Exception("Hoạt động này chưa mở đăng ký.");

            // 4. Kiểm tra account có phải member active của CLB không
            // CHỈ MEMBER mới được đăng ký tham gia activity
            // Student muốn tham gia activity thì phải trở thành member của CLB đó trước
            var memberships = await _membershipRepo.GetMembershipsAsync(accountId);
            var membership = memberships.FirstOrDefault(m => m.ClubId == activity.ClubId && m.Status == "active");
            
            if (membership == null)
            {
                // Kiểm tra xem có request pending không để đưa ra thông báo phù hợp
                var hasPendingRequest = await _membershipRequestRepo.HasPendingRequestAsync(accountId, activity.ClubId);
                if (hasPendingRequest)
                {
                    throw new Exception("Bạn chưa phải thành viên của câu lạc bộ này. Yêu cầu tham gia của bạn đang chờ duyệt.");
                }
                else
                {
                    throw new Exception("Bạn chưa phải thành viên của câu lạc bộ này. Vui lòng gửi yêu cầu tham gia câu lạc bộ trước.");
                }
            }

            // 5. Kiểm tra đã đăng ký chưa
            var existingParticipants = await _participantRepo.GetByMembershipIdAsync(membership.Id);
            var existingParticipant = existingParticipants.FirstOrDefault(p => p.ActivityId == activityId);
            
            if (existingParticipant != null)
            {
                if (existingParticipant.Attended == true)
                    throw new Exception("Bạn đã đăng ký tham gia hoạt động này rồi (trạng thái: attend).");
                else if (existingParticipant.Attended == false)
                    throw new Exception("Bạn đã hủy đăng ký hoạt động này rồi. Không thể đăng ký lại.");
            }

            // 6. Tạo participant với trạng thái "attend" ngay (không cần club leader duyệt)
            var participant = new ActivityParticipant
            {
                ActivityId = activityId,
                MembershipId = membership.Id,
                RegisterTime = DateTime.UtcNow,
                Attended = true // Set "attend" ngay khi đăng ký
            };

            await _participantRepo.AddParticipantAsync(participant);
            await _participantRepo.SaveAsync();
        }

        /// <summary>
        /// Hủy đăng ký tham gia activity.
        /// 
        /// API: DELETE /api/student/activities/{activityId}/cancel
        /// Luồng: Cập nhật Attended = false trong ActivityParticipant.
        /// </summary>
        public async Task CancelRegistrationAsync(int accountId, int activityId, string? reason)
        {
            // Kiểm tra activity tồn tại
            var activity = await _activityRepo.GetByIdAsync(activityId);
            if (activity == null) throw new Exception("Hoạt động không tồn tại");

            var club = await _clubRepo.GetByIdAsync(activity.ClubId);  // THÊM
            if (club.Status == "Locked") throw new Exception("Không thể hủy khi câu lạc bộ đang bị khóa");  // THÊM

            // Kiểm tra account có phải member của CLB không
            var memberships = await _membershipRepo.GetMembershipsAsync(accountId);
            var membership = memberships.FirstOrDefault(m => m.ClubId == activity.ClubId && m.Status == "active");
            if (membership == null)
                throw new Exception("Bạn không phải thành viên của câu lạc bộ này.");

            // Tìm participant
            var participants = await _participantRepo.GetByMembershipIdAsync(membership.Id);
            var participant = participants.FirstOrDefault(p => p.ActivityId == activityId);
            if (participant == null)
                throw new Exception("Bạn chưa đăng ký tham gia hoạt động này.");

            // Kiểm tra đã cancel chưa
            if (participant.Attended == false)
                throw new Exception("Bạn đã hủy đăng ký hoạt động này rồi.");

            // Set trạng thái "cancel" và lưu reason
            participant.Attended = false; // false = cancel
            //participant.CancelReason = reason; // Lưu lý do hủy
            
            await _participantRepo.UpdateParticipantAsync(participant);
            await _participantRepo.SaveAsync();
        }

        /// <summary>
        /// Xem lịch sử tham gia activity của bản thân (đã đăng ký, đã hủy, đã tham gia...).
        /// 
        /// API: GET /api/student/activities/history
        /// </summary>
        public async Task<List<ActivityParticipantDto>> GetMyActivityHistoryAsync(int accountId)
        {
            // Lấy tất cả memberships của student
            var memberships = await _membershipRepo.GetMembershipsAsync(accountId);
            var result = new List<ActivityParticipantDto>();

            foreach (var membership in memberships)
            {
                var participants = await _participantRepo.GetByMembershipIdAsync(membership.Id);
                foreach (var participant in participants)
                {
                    if (participant.Activity != null)
                    {
                        result.Add(new ActivityParticipantDto
                        {
                            Id = participant.Id,
                            ActivityId = participant.ActivityId,
                            ActivityTitle = participant.Activity.Title ?? "",
                            ClubId = participant.Activity.ClubId,
                            ClubName = participant.Activity.Club?.Name ?? "",
                            StartTime = participant.Activity.StartTime,
                            EndTime = participant.Activity.EndTime,
                            Location = participant.Activity.Location ?? "",
                            RegisterTime = participant.RegisterTime,
                            Attended = participant.Attended,
                            //CancelReason = participant.CancelReason,
                            ActivityStatus = participant.Activity.Status ?? ""
                        });
                    }
                }
            }

            return result.OrderByDescending(r => r.RegisterTime).ToList();
        }

        /// <summary>
        /// Lấy danh sách activity mà sinh viên (là member của ít nhất một CLB) có thể đăng ký.
        /// Chỉ hiển thị activity đang mở đăng ký (Status = "Active" hoặc "opened").
        /// 
        /// API: GET /api/student/activities/for-registration
        /// </summary>
        // Lấy activities mà student (đã là member) có thể đăng ký
        public async Task<List<ActivityDto>> GetActivitiesForRegistrationAsync(int accountId)
        {
            // Lấy tất cả memberships của student
            var memberships = await _membershipRepo.GetMembershipsAsync(accountId);
            var clubIds = memberships.Select(m => m.ClubId).ToList();

            if (!clubIds.Any())
                return new List<ActivityDto>();

            // Lấy tất cả activities của các CLB mà student là member
            // Chỉ hiển thị activities có status "Active" hoặc "opened" (đang mở đăng ký) để member có thể đăng ký
            var allActivities = await _activityRepo.GetAllAsync();
            var now = DateTimeExtensions.NowVietnam();
            var availableActivities = allActivities
                .Where(a => clubIds.Contains(a.ClubId) && 
                           (a.Status == "Active" || a.Status == "opened") && // Chỉ hiển thị activities đang mở đăng ký
                           (!a.StartTime.HasValue || a.StartTime.Value > now)) // Chưa bắt đầu
                .ToList();

            return availableActivities.Select(a => new ActivityDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title ?? "",
                Description = a.Description ?? "",
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Location = a.Location ?? "",
                Status = CalculateActivityStatus(a), // Tính status động
                CreatedBy = a.CreatedBy
            }).ToList();
        }

        /// <summary>
        /// Student xem tất cả activities "Active" của tất cả CLB (không cần là member) - chỉ để xem.
        /// - Luồng: Front-end gọi GET api/student/activities/view-all -> Controller GetAllActivitiesForViewing -> Gọi method này.
        ///   Lấy từ DB bảng Activity, lọc Status="Active"/"opened", không lưu mới.
        /// - Tương tác: Không yêu cầu membership, nhưng để register thì phải gọi API membership trước.
        /// </summary>
        // Student xem tất cả activities "Active" của tất cả CLB (không cần là member) - chỉ để xem
        public async Task<List<ActivityDto>> GetAllActivitiesForViewingAsync()
        {
            var allActivities = await _activityRepo.GetAllAsync();
            
            // Debug: Log số lượng activities
            Console.WriteLine($"[GetAllActivitiesForViewingAsync] Total activities from DB: {allActivities.Count}");
            if (allActivities.Any())
            {
                Console.WriteLine($"[GetAllActivitiesForViewingAsync] Sample statuses: {string.Join(", ", allActivities.Take(5).Select(a => a.Status))}");
            }
            
            // Chỉ hiển thị activities có status "Active" hoặc "opened" (đang mở đăng ký)
            // Không lọc theo StartTime vì đây là endpoint để xem, không phải để đăng ký
            // Sử dụng case-insensitive comparison để tránh vấn đề với case
            var activities = allActivities
                .Where(a => a.Status != null && 
                           (a.Status.Equals("Active", StringComparison.OrdinalIgnoreCase) || 
                            a.Status.Equals("opened", StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(a => a.StartTime)
                .ToList();
            
            Console.WriteLine($"[GetAllActivitiesForViewingAsync] Filtered activities (Active/opened): {activities.Count}");
            
            return activities.Select(a => new ActivityDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title ?? "",
                Description = a.Description ?? "",
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Location = a.Location ?? "",
                Status = CalculateActivityStatus(a), // Tính status động
                CreatedBy = a.CreatedBy
            }).ToList();
        }

        /// <summary>
        /// Xem activity của một CLB cụ thể (không cần là member - chỉ để xem).
        /// 
        /// API: GET /api/student/activities/view-club/{clubId}
        /// </summary>
        public async Task<List<ActivityDto>> GetActivitiesByClubForViewingAsync(int clubId)
        {
            var activities = await _activityRepo.GetByClubAsync(clubId);
            
            // Chỉ hiển thị activities có status "Active" hoặc "opened" (đang mở đăng ký)
            // Không lọc theo StartTime vì đây là endpoint để xem, không phải để đăng ký
            var filteredActivities = activities
                .Where(a => a.Status == "Active" || a.Status == "opened")
                .OrderByDescending(a => a.StartTime)
                .ToList();

            return filteredActivities.Select(a => new ActivityDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                Title = a.Title ?? "",
                Description = a.Description ?? "",
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Location = a.Location ?? "",
                Status = CalculateActivityStatus(a), // Tính status động
                CreatedBy = a.CreatedBy
            }).ToList();
        }

        // Tính status của activity dựa trên thời gian hiện tại
        private static string CalculateActivityStatus(Activity a)
        {
            var now = DateTimeExtensions.NowVietnam();

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
                // Chỉ tự động set Completed nếu status hiện tại là Active hoặc Active_Closed hoặc Ongoing
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


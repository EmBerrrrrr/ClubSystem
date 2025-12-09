using DTO.DTO.Activity;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class StudentActivityService : IStudentActivityService
    {
        private readonly IActivityParticipantRepository _participantRepo;
        private readonly IActivityRepository _activityRepo;
        private readonly IMembershipRepository _membershipRepo;
        private readonly IMembershipRequestRepository _membershipRequestRepo;

        public StudentActivityService(
            IActivityParticipantRepository participantRepo,
            IActivityRepository activityRepo,
            IMembershipRepository membershipRepo,
            IMembershipRequestRepository membershipRequestRepo)
        {
            _participantRepo = participantRepo;
            _activityRepo = activityRepo;
            _membershipRepo = membershipRepo;
            _membershipRequestRepo = membershipRequestRepo;
        }

        public async Task RegisterForActivityAsync(int accountId, int activityId)
        {
            // 1. Kiểm tra activity tồn tại
            var activity = await _activityRepo.GetByIdAsync(activityId);
            if (activity == null)
                throw new Exception("Hoạt động không tồn tại.");

            // 2. Kiểm tra activity không bị hủy hoặc đã hoàn thành
            if (activity.Status == "Cancelled")
                throw new Exception("Hoạt động này đã bị hủy.");
            
            if (activity.Status == "Completed")
                throw new Exception("Hoạt động này đã hoàn thành.");

            // 3. Kiểm tra thời gian: Không cho đăng ký sau khi activity đã bắt đầu
            if (activity.StartTime.HasValue && activity.StartTime.Value <= DateTime.Now)
                throw new Exception("Hoạt động này đã bắt đầu, không thể đăng ký thêm.");

            // 4. Kiểm tra student có phải member của CLB không
            // Chỉ có member active mới được đăng ký tham gia activity
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
            if (await _participantRepo.IsRegisteredAsync(activityId, membership.Id))
                throw new Exception("Bạn đã đăng ký tham gia hoạt động này rồi.");

            // 6. Tạo participant
            var participant = new ActivityParticipant
            {
                ActivityId = activityId,
                MembershipId = membership.Id,
                RegisterTime = DateTime.Now,
                Attended = null
            };

            await _participantRepo.AddParticipantAsync(participant);
            await _participantRepo.SaveAsync();
        }

        public async Task CancelRegistrationAsync(int accountId, int activityId)
        {
            // Kiểm tra activity tồn tại
            var activity = await _activityRepo.GetByIdAsync(activityId);
            if (activity == null)
                throw new Exception("Hoạt động không tồn tại.");

            // Kiểm tra student có phải member của CLB không
            var memberships = await _membershipRepo.GetMembershipsAsync(accountId);
            var membership = memberships.FirstOrDefault(m => m.ClubId == activity.ClubId && m.Status == "active");
            if (membership == null)
                throw new Exception("Bạn không phải thành viên của câu lạc bộ này.");

            // Tìm participant
            var participants = await _participantRepo.GetByMembershipIdAsync(membership.Id);
            var participant = participants.FirstOrDefault(p => p.ActivityId == activityId);
            if (participant == null)
                throw new Exception("Bạn chưa đăng ký tham gia hoạt động này.");

            // Xóa registration (hoặc có thể set flag thay vì xóa)
            // Ở đây tôi sẽ xóa luôn
            // Nếu muốn giữ lại lịch sử, có thể thêm field IsCancelled
            // Tạm thời xóa để đơn giản
            throw new Exception("Chức năng hủy đăng ký chưa được implement. Vui lòng liên hệ admin.");
        }

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
                            ActivityStatus = participant.Activity.Status ?? ""
                        });
                    }
                }
            }

            return result.OrderByDescending(r => r.RegisterTime).ToList();
        }

        public async Task<List<ActivityDto>> GetAvailableActivitiesForMyClubsAsync(int accountId)
        {
            // Lấy tất cả memberships của student
            var memberships = await _membershipRepo.GetMembershipsAsync(accountId);
            var clubIds = memberships.Select(m => m.ClubId).ToList();

            if (!clubIds.Any())
                return new List<ActivityDto>();

            // Lấy tất cả activities của các CLB mà student là member
            // ClubLeader tạo activity thì tự động hiển thị cho student đăng ký (không cần duyệt)
            // Chỉ loại bỏ các activity đã bị hủy, đã hoàn thành, hoặc đã bắt đầu
            var allActivities = await _activityRepo.GetAllAsync();
            var now = DateTime.Now;
            var availableActivities = allActivities
                .Where(a => clubIds.Contains(a.ClubId) && 
                           a.Status != "Cancelled" && 
                           a.Status != "Completed" &&
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
                Status = a.Status ?? "",
                CreatedBy = a.CreatedBy
            }).ToList();
        }
    }
}


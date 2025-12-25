using Azure.Core;
using DTO.DTO.Membership;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Helper;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class ClubLeaderMembershipService : IClubLeaderMembershipService
    {
        private readonly IMembershipRequestRepository _reqRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IMembershipRepository _membershipRepo;
        private readonly IPaymentRepository _paymentRepo;
        private static readonly Random _rnd = new Random();
        private readonly INotificationService _noti;

        public ClubLeaderMembershipService(
            IMembershipRequestRepository reqRepo,
            IClubRepository clubRepo,
            IMembershipRepository membershipRepo,
            IPaymentRepository paymentRepo,
            INotificationService noti) 
        {
            _reqRepo = reqRepo;
            _clubRepo = clubRepo;
            _membershipRepo = membershipRepo;
            _paymentRepo = paymentRepo;
            _noti = noti;
        }

        // Lấy các request pending của tất cả CLB mà leader này quản lý
        public async Task<List<MembershipRequestForLeaderDto>> GetPendingRequestsAsync(int leaderId)
        {
            var myClubs = await _clubRepo.GetByLeaderIdAsync(leaderId);
            var requests = new List<MembershipRequest>();

            foreach (var club in myClubs)
            {
                var list = await _reqRepo.GetPendingRequestsByClubAsync(club.Id);
                requests.AddRange(list);
            }

            return requests.Select(r => new MembershipRequestForLeaderDto
            {
                Id = r.Id,
                AccountId = r.AccountId,
                ClubId = r.ClubId,
                Status = r.Status,
                Note = r.Note,
                RequestDate = r.RequestDate,
                FullName = r.Account?.FullName,
                Email = r.Account?.Email,
                Phone = r.Account?.Phone,
                Reason = r.Note, // Lý do tham gia được lưu trong Note
                Major = r.Major,
                Skills = r.Skills
            }).ToList();
        }

        // Lấy tất cả requests (với các trạng thái) của tất cả CLB mà leader này quản lý
        public async Task<List<MembershipRequestForLeaderDto>> GetAllRequestsAsync(int leaderId)
        {
            var myClubs = await _clubRepo.GetByLeaderIdAsync(leaderId);
            var requests = new List<MembershipRequest>();

            foreach (var club in myClubs)
            {
                var list = await _reqRepo.GetAllRequestsByClubAsync(club.Id);
                requests.AddRange(list);
            }

            return requests.Select(r => new MembershipRequestForLeaderDto
            {
                Id = r.Id,
                AccountId = r.AccountId,
                ClubId = r.ClubId,
                Status = r.Status,
                Note = r.Note,
                RequestDate = r.RequestDate,
                FullName = r.Account?.FullName,
                Email = r.Account?.Email,
                Phone = r.Account?.Phone,
                Reason = r.Note, // Lý do tham gia được lưu trong Note
                Major = r.Major,
                Skills = r.Skills
            }).ToList();
        }

        // Lấy danh sách thành viên của tất cả CLB mà leader này quản lý
        public async Task<List<ClubMemberDto>> GetClubMembersAsync(int leaderId)
        {
            var myClubs = await _clubRepo.GetByLeaderIdAsync(leaderId);
            var allMembers = new List<Membership>();

            foreach (var club in myClubs)
            {
                var members = await _membershipRepo.GetMembershipsByClubIdAsync(club.Id);
                allMembers.AddRange(members);
            }

            return allMembers.Select(m => new ClubMemberDto
            {
                Member = new MemberInfo
                {
                    AccountId = m.AccountId,
                    FullName = m.Account?.FullName,
                    Email = m.Account?.Email,
                    Phone = m.Account?.Phone,
                    Status = m.Status ?? ""
                },
                MembershipId = m.Id,
                ClubId = m.ClubId,
                JoinDate = m.JoinDate
            }).ToList();
        }

        // Lấy danh sách thành viên của một CLB cụ thể mà leader quản lý
        public async Task<List<ClubMemberDto>> GetClubMembersByClubIdAsync(int leaderId, int clubId)
        {
            // Kiểm tra leader có quyền với CLB này không
            if (!await _clubRepo.IsLeaderOfClubAsync(clubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            var members = await _membershipRepo.GetMembershipsByClubIdAsync(clubId);

            return members.Select(m => new ClubMemberDto
            {
                Member = new MemberInfo
                {
                    AccountId = m.AccountId,
                    FullName = m.Account?.FullName,
                    Email = m.Account?.Email,
                    Phone = m.Account?.Phone,
                    Status = m.Status ?? ""
                },
                MembershipId = m.Id,
                ClubId = m.ClubId,
                JoinDate = m.JoinDate
            }).ToList();
        }
        public async Task ApproveAsync(int leaderId, int requestId, string? note)
        {
            var req = await _reqRepo.GetByIdAsync(requestId);
            if (req == null) throw new Exception("Request not found");

            var clubs = await _clubRepo.GetByIdAsync(req.ClubId)  // THÊM
                ?? throw new Exception("Không tìm thấy CLB");
            if (clubs.Status == "Locked") throw new Exception("Cannot approve request for locked club");  // THÊM

            if (!await _clubRepo.IsLeaderOfClubAsync(req.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            var club = await _clubRepo.GetByIdAsync(req.ClubId)
                ?? throw new Exception("Không tìm thấy CLB");

            var membership = new Membership
            {
                AccountId = req.AccountId,
                ClubId = req.ClubId,
                // DÙNG GIỜ VIỆT NAM
                JoinDate = DateOnly.FromDateTime(DateTimeExtensions.NowVietnam()),
                Status = "pending_payment"
            };

            await _membershipRepo.AddMembershipAsync(membership);
            await _membershipRepo.SaveAsync();
            var orderCode = await GenerateUniqueOrderCodeAsync();
            var payment = new Payment
            {
                MembershipId = membership.Id,
                ClubId = club.Id,
                AccountId = membership.AccountId,
                Amount = club.MembershipFee ?? 0,
                Status = "created",  
                Method = "payos",
                OrderCode = null     
            };

            await _paymentRepo.AddAsync(payment);
            await _paymentRepo.SaveAsync();


            req.Status = "Awaiting Payment";
            req.ProcessedBy = leaderId;
            req.ProcessedAt = DateTime.UtcNow;
            req.Note = note;

            await _reqRepo.UpdateAsync(req);

            _noti.Push(
                req.AccountId,
                "Được duyệt vào CLB 🎉",
                $"Bạn đã được chấp nhận vào CLB {club.Name}"
            );
        }

        private async Task<int> GenerateUniqueOrderCodeAsync()
        {
            const int maxRetry = 5;
            for (int i = 0; i < maxRetry; i++)
            {
                var code = _rnd.Next(100000000, 999999999);

                var exists = await _paymentRepo.ExistsOrderCodeAsync(code);
                if (!exists)
                    return code;
            }

            throw new Exception("Không tạo được OrderCode duy nhất, vui lòng thử lại.");
        }

        public async Task RejectAsync(int leaderId, int requestId, string? note)
        {
            var req = await _reqRepo.GetByIdAsync(requestId);
            if (req == null) throw new Exception("Request not found");

            var club = await _clubRepo.GetByIdAsync(req.ClubId)  // THÊM
                ?? throw new Exception("Không tìm thấy CLB");
            if (club.Status != null && club.Status.ToLower() == "locked") throw new Exception("Cannot reject request for locked club");  // THÊM

            req.Status = "Reject";
            req.ProcessedBy = leaderId;
            req.ProcessedAt = DateTime.UtcNow;
            req.Note = note;

            await _reqRepo.UpdateAsync(req);

            _noti.Push(
                req.AccountId,
                "Bị từ chối gia nhập CLB",
                note ?? "Yêu cầu của bạn đã bị từ chối"
            );
        }

        // Khóa member (set status = "locked")
        public async Task LockMemberAsync(int leaderId, int membershipId, string? reason)
        {
            var membership = await _membershipRepo.GetMembershipByIdAsync(membershipId)
                            ?? throw new Exception("Không tìm thấy thành viên.");

            var club = await _clubRepo.GetByIdAsync(membership.ClubId);  // THÊM
            if (club == null) throw new Exception("Không tìm thấy CLB.");
            if (club.Status == "Locked") throw new Exception("Cannot lock member in locked club");  // THÊM

            // Kiểm tra leader có quyền với CLB này không
            if (!await _clubRepo.IsLeaderOfClubAsync(membership.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (membership.Status != null && membership.Status.ToLower() == "locked")
                throw new Exception("Thành viên đã bị khóa.");

            membership.Status = "locked";
            _membershipRepo.UpdateMembership(membership);
            await _membershipRepo.SaveAsync();
        }

        // Mở khóa member (set status = "active")
        public async Task UnlockMemberAsync(int leaderId, int membershipId)
        {
            var membership = await _membershipRepo.GetMembershipByIdAsync(membershipId)
                            ?? throw new Exception("Không tìm thấy thành viên.");

            var club = await _clubRepo.GetByIdAsync(membership.ClubId);  // THÊM
            if (club == null) throw new Exception("Không tìm thấy CLB.");
            if (club.Status == "Locked") throw new Exception("Cannot unlock member in locked club");  // THÊM

            if (membership.Status == null || membership.Status.ToLower() != "locked")
                throw new Exception("Thành viên không ở trạng thái bị khóa.");

            membership.Status = "active";
            _membershipRepo.UpdateMembership(membership);
            await _membershipRepo.SaveAsync();
        }

        // Hủy/Remove member khỏi CLB (XÓA CỨNG - hard delete)
        // Hủy/Remove member khỏi CLB (XÓA CỨNG + gửi thông báo kèm lý do)
        public async Task RemoveMemberAsync(int leaderId, int membershipId, string? reason)
        {
            var membership = await _membershipRepo.GetMembershipByIdAsync(membershipId)
                            ?? throw new Exception("Không tìm thấy thành viên.");

            var club = await _clubRepo.GetByIdAsync(membership.ClubId);
            if (club == null) throw new Exception("Không tìm thấy CLB.");
            if (club.Status == "Locked")
                throw new Exception("Cannot remove member from locked club");

            // Kiểm tra quyền leader
            if (!await _clubRepo.IsLeaderOfClubAsync(membership.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            // 1. Xóa cứng membership khỏi DB
            await _membershipRepo.DeleteMembership(membership);
            await _membershipRepo.SaveAsync();

            // 2. Gửi thông báo cho sinh viên bị remove (lý do được lưu trong noti in-memory)
            string title = $"Bạn đã bị xóa khỏi CLB \"{club.Name}\"";
            string message = !string.IsNullOrWhiteSpace(reason)
                ? $"Lý do: {reason.Trim()}"
                : "CLB không cung cấp lý do cụ thể. Vui lòng liên hệ leader để biết thêm thông tin.";

            _noti.Push(membership.AccountId, title, message);
        }

        public async Task NotifyLeaderWhenRequestCreated(int clubId, int studentId)
        {
            var leaderIds = await _clubRepo.GetLeaderAccountIdsByClubIdAsync(clubId);

            foreach (var leaderId in leaderIds)
            {
                _noti.Push(
                    leaderId,
                    "Đơn xin gia nhập CLB",
                    "Có sinh viên mới xin gia nhập CLB bạn quản lý"
                );
            }
        }
    }

}

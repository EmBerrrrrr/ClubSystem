using DTO.DTO.Membership;
using Repository.Models;
using Repository.Repo.Interfaces;
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

        public ClubLeaderMembershipService(
            IMembershipRequestRepository reqRepo,
            IClubRepository clubRepo,
            IMembershipRepository membershipRepo,
            IPaymentRepository paymentRepo) 
        {
            _reqRepo = reqRepo;
            _clubRepo = clubRepo;
            _membershipRepo = membershipRepo;
            _paymentRepo = paymentRepo; 
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
                Reason = r.Note // Lý do tham gia được lưu trong Note
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
                MembershipId = m.Id,
                AccountId = m.AccountId,
                ClubId = m.ClubId,
                FullName = m.Account?.FullName,
                Email = m.Account?.Email,
                Phone = m.Account?.Phone,
                JoinDate = m.JoinDate,
                Status = m.Status
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
                MembershipId = m.Id,
                AccountId = m.AccountId,
                ClubId = m.ClubId,
                FullName = m.Account?.FullName,
                Email = m.Account?.Email,
                Phone = m.Account?.Phone,
                JoinDate = m.JoinDate,
                Status = m.Status
            }).ToList();
        }
        public async Task ApproveAsync(int leaderId, int requestId, string? note)
        {
            var req = await _reqRepo.GetByIdAsync(requestId)
                ?? throw new Exception("Không tìm thấy request.");

            // kiểm tra leader có quyền với CLB không
            if (!await _clubRepo.IsLeaderOfClubAsync(req.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (req.Status != "pending")
                throw new Exception("Yêu cầu đã được xử lý.");

            // Lấy thông tin CLB để biết membership fee
            var club = await _clubRepo.GetByIdAsync(req.ClubId)
                ?? throw new Exception("Không tìm thấy CLB.");

            var fee = club.MembershipFee ?? 0;

            // 1) Tạo membership với trạng thái pending_payment
            var membership = new Membership
            {
                AccountId = req.AccountId,
                ClubId = req.ClubId,
                JoinDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "pending_payment"
            };

            await _membershipRepo.AddMembershipAsync(membership);
            await _membershipRepo.SaveAsync();

            // 2) Tạo orderCode (sau này dùng đúng code này khi gọi PayOS)
            long orderCode = _rnd.Next(100000000, 999999999);

            // 3) Tạo payment (status = pending)
            var payment = new Payment
            {
                MembershipId = membership.Id,
                ClubId = req.ClubId,
                Amount = fee,
                Method = "payos",      
                Status = "pending", 
                OrderCode = orderCode,
                Description = $"Membership fee for club {club.Name} - member {membership.Id}"
            };

            await _paymentRepo.AddAsync(payment);
            await _paymentRepo.SaveAsync();

            // 4) Cập nhật request
            req.Status = "approved_pending_payment";
            req.ProcessedBy = leaderId;
            req.ProcessedAt = DateTime.UtcNow;
            req.Note = note;

            await _reqRepo.UpdateAsync(req);

            // (tùy bạn) return orderCode / payment info qua controller để FE hiển thị link PayOS
        }


        public async Task RejectAsync(int leaderId, int requestId, string? note)
        {
            var req = await _reqRepo.GetByIdAsync(requestId)
                ?? throw new Exception("Không tìm thấy request.");

            if (!await _clubRepo.IsLeaderOfClubAsync(req.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (req.Status != "pending")
                throw new Exception("Yêu cầu đã được xử lý.");

            req.Status = "rejected";
            req.ProcessedBy = leaderId;
            req.ProcessedAt = DateTime.Now;
            req.Note = note;

            await _reqRepo.UpdateAsync(req);
        }

        // Khóa member (set status = "locked")
        public async Task LockMemberAsync(int leaderId, int membershipId, string? reason)
        {
            var membership = await _membershipRepo.GetMembershipByIdAsync(membershipId)
                ?? throw new Exception("Không tìm thấy thành viên.");

            // Kiểm tra leader có quyền với CLB này không
            if (!await _clubRepo.IsLeaderOfClubAsync(membership.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (membership.Status == "locked")
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

            // Kiểm tra leader có quyền với CLB này không
            if (!await _clubRepo.IsLeaderOfClubAsync(membership.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (membership.Status != "locked")
                throw new Exception("Thành viên không ở trạng thái bị khóa.");

            membership.Status = "active";
            _membershipRepo.UpdateMembership(membership);
            await _membershipRepo.SaveAsync();
        }

        // Hủy/Remove member khỏi CLB (set status = "removed")
        public async Task RemoveMemberAsync(int leaderId, int membershipId, string? reason)
        {
            var membership = await _membershipRepo.GetMembershipByIdAsync(membershipId)
                ?? throw new Exception("Không tìm thấy thành viên.");

            // Kiểm tra leader có quyền với CLB này không
            if (!await _clubRepo.IsLeaderOfClubAsync(membership.ClubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            if (membership.Status == "removed")
                throw new Exception("Thành viên đã bị hủy khỏi CLB.");

            membership.Status = "removed";
            _membershipRepo.UpdateMembership(membership);
            await _membershipRepo.SaveAsync();
        }
    }

}

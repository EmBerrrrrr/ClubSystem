using DTO.DTO.Payment;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class ClubLeaderPaymentService : IClubLeaderPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IMembershipRepository _membershipRepo;
        private readonly IClubRepository _clubRepo;
        private readonly IMembershipRequestRepository _membershipRequestRepo;

        public ClubLeaderPaymentService(
            IPaymentRepository paymentRepo,
            IMembershipRepository membershipRepo,
            IClubRepository clubRepo,
            IMembershipRequestRepository membershipRequestRepo)
        {
            _paymentRepo = paymentRepo;
            _membershipRepo = membershipRepo;
            _clubRepo = clubRepo;
            _membershipRequestRepo = membershipRequestRepo;
        }

        public async Task<List<ClubPaymentDto>> GetClubPaymentsAsync(int leaderId, int clubId)
        {
            // Kiểm tra leader có quyền quản lý CLB này không
            var isLeader = await _clubRepo.IsLeaderOfClubAsync(clubId, leaderId);
            if (!isLeader)
                throw new UnauthorizedAccessException("Bạn không có quyền quản lý câu lạc bộ này.");

            // Lấy tất cả payments của CLB
            var payments = await _paymentRepo.GetPaymentsByClubIdAsync(clubId);

            return payments.Select(p => new ClubPaymentDto
            {
                Id = p.Id,
                MembershipId = p.MembershipId,
                AccountId = p.Membership?.AccountId ?? 0,
                MemberName = p.Membership?.Account?.FullName ?? "N/A",
                MemberEmail = p.Membership?.Account?.Email,
                MemberPhone = p.Membership?.Account?.Phone,
                Amount = p.Amount,
                PaidDate = p.PaidDate,
                Method = p.Method ?? "",
                Status = p.Status ?? ""
            }).ToList();
        }

        public async Task<List<MemberPaymentDto>> GetMemberPaymentsAsync(int leaderId, int clubId, int accountId)
        {
            // Kiểm tra leader có quyền quản lý CLB này không
            var isLeader = await _clubRepo.IsLeaderOfClubAsync(clubId, leaderId);
            if (!isLeader)
                throw new UnauthorizedAccessException("Bạn không có quyền quản lý câu lạc bộ này.");

            // Kiểm tra account có phải member của CLB không
            var membership = await _membershipRepo.GetMembershipByAccountAndClubAsync(accountId, clubId);
            if (membership == null)
                throw new Exception("Người dùng này không phải thành viên của câu lạc bộ.");

            // Lấy tất cả payments của member này trong CLB
            var allPayments = await _paymentRepo.GetPaymentsByClubIdAsync(clubId);
            var memberPayments = allPayments
                .Where(p => p.Membership?.AccountId == accountId)
                .OrderByDescending(p => p.PaidDate)
                .ToList();

            return memberPayments.Select(p => new MemberPaymentDto
            {
                Id = p.Id,
                MembershipId = p.MembershipId,
                Amount = p.Amount,
                PaidDate = p.PaidDate,
                Method = p.Method ?? "",
                Status = p.Status ?? ""
            }).ToList();
        }

        public async Task<List<DebtMemberDto>> GetDebtMembersAsync(int leaderId, int clubId)
        {
            // Kiểm tra leader có quyền quản lý CLB này không
            var isLeader = await _clubRepo.IsLeaderOfClubAsync(clubId, leaderId);
            if (!isLeader)
                throw new UnauthorizedAccessException("Bạn không có quyền quản lý câu lạc bộ này.");

            // Lấy thông tin CLB
            var club = await _clubRepo.GetByIdAsync(clubId);
            if (club == null)
                throw new Exception("Câu lạc bộ không tồn tại.");

            // Nếu CLB không có phí thành viên
            if (club.MembershipFee == null || club.MembershipFee == 0)
                return new List<DebtMemberDto>();

            var debtMembers = new List<DebtMemberDto>();

            // Lấy tất cả members của CLB
            var memberships = await _membershipRepo.GetMembershipsByClubIdAsync(clubId);

            // Lấy tất cả payments của CLB
            var allPayments = await _paymentRepo.GetPaymentsByClubIdAsync(clubId);

            foreach (var membership in memberships)
            {
                // Lấy payment gần nhất của member này
                var memberPayments = allPayments
                    .Where(p => p.Membership?.AccountId == membership.AccountId)
                    .OrderByDescending(p => p.PaidDate ?? DateTime.MinValue)
                    .ToList();

                var latestPayment = memberPayments.FirstOrDefault();

                // Kiểm tra member có nợ phí không
                bool hasDebt = false;
                int? pendingPaymentId = null;

                if (membership.Status == "pending_payment")
                {
                    // Member đang chờ thanh toán
                    hasDebt = true;
                    pendingPaymentId = latestPayment?.Id;
                }
                else if (membership.Status == "active")
                {
                    // Kiểm tra xem có payment nào đã paid chưa
                    var hasPaidPayment = memberPayments.Any(p => p.Status == "paid");
                    if (!hasPaidPayment)
                    {
                        // Member active nhưng chưa có payment paid
                        // Kiểm tra xem có request đã approve nhưng chưa thanh toán không
                        var requests = await _membershipRequestRepo.GetRequestsOfAccountAsync(membership.AccountId);
                        var approvedRequest = requests.FirstOrDefault(r => 
                            r.ClubId == clubId && r.Status == "approved_pending_payment");
                        
                        if (approvedRequest != null)
                        {
                            // Có request đã approve nhưng chưa thanh toán
                            hasDebt = true;
                            pendingPaymentId = latestPayment?.Id;
                        }
                        // Nếu không có request approved_pending_payment nhưng cũng không có payment paid
                        // thì có thể là member được tạo trực tiếp mà chưa thanh toán
                        // Trong trường hợp này, nếu CLB có phí thì cũng coi là nợ
                        else if (memberPayments.Count == 0)
                        {
                            // Không có payment nào cả - có thể là member được tạo trực tiếp
                            // Nếu CLB có phí thì coi là nợ
                            hasDebt = true;
                        }
                    }
                }

                if (hasDebt)
                {
                    debtMembers.Add(new DebtMemberDto
                    {
                        AccountId = membership.AccountId,
                        MemberName = membership.Account?.FullName ?? "N/A",
                        MemberEmail = membership.Account?.Email,
                        MemberPhone = membership.Account?.Phone,
                        MembershipId = membership.Id,
                        MembershipStatus = membership.Status ?? "",
                        DebtAmount = club.MembershipFee ?? 0,
                        JoinDate = membership.JoinDate.HasValue 
                            ? membership.JoinDate.Value.ToDateTime(TimeOnly.MinValue) 
                            : (DateTime?)null,
                        PendingPaymentId = pendingPaymentId
                    });
                }
            }

            return debtMembers.OrderBy(d => d.MemberName).ToList();
        }
    }
}


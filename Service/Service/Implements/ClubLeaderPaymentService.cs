using DTO.DTO.Payment;
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
        private readonly IClubRepository _clubRepo;

        public ClubLeaderPaymentService(
            IPaymentRepository paymentRepo,
            IClubRepository clubRepo)
        {
            _paymentRepo = paymentRepo;
            _clubRepo = clubRepo;
        }

        // Xem payments theo CLB
        public async Task<List<PaymentDto>> GetPaymentsByClubAsync(int leaderId, int clubId)
        {
            // Kiểm tra leader có quyền với CLB này không
            if (!await _clubRepo.IsLeaderOfClubAsync(clubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            var payments = await _paymentRepo.GetPaymentsByClubIdAsync(clubId);

            return payments.Select(p => new PaymentDto
            {
                Id = p.Id,
                MembershipId = p.MembershipId,
                ClubId = p.ClubId,
                ClubName = p.Club?.Name ?? "",
                Amount = p.Amount,
                PaidDate = p.PaidDate,
                Method = p.Method ?? "",
                Status = p.Status ?? "",
                OrderCode = p.OrderCode,
                Description = p.Description ?? "",
                AccountId = p.Membership?.AccountId ?? 0,
                FullName = p.Membership?.Account?.FullName ?? "",
                Email = p.Membership?.Account?.Email ?? "",
                Phone = p.Membership?.Account?.Phone ?? ""
            }).ToList();
        }

        // Xem ai còn nợ phí
        public async Task<List<DebtorDto>> GetDebtorsByClubAsync(int leaderId, int clubId)
        {
            // Kiểm tra leader có quyền với CLB này không
            if (!await _clubRepo.IsLeaderOfClubAsync(clubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            var pendingPayments = await _paymentRepo.GetPendingPaymentsByClubIdAsync(clubId);

            return pendingPayments.Select(p => new DebtorDto
            {
                MembershipId = p.MembershipId,
                AccountId = p.Membership?.AccountId ?? 0,
                FullName = p.Membership?.Account?.FullName ?? "",
                Email = p.Membership?.Account?.Email ?? "",
                Phone = p.Membership?.Account?.Phone ?? "",
                ClubId = p.ClubId,
                ClubName = p.Club?.Name ?? "",
                Amount = p.Amount,
                JoinDate = p.Membership?.JoinDate,
                PaymentId = p.Id,
                PaymentStatus = p.Status ?? "",
                PaymentCreatedDate = p.PaidDate
            }).ToList();
        }

        // Lịch sử thanh toán (tất cả các trạng thái: paid, pending, failed, etc.)
        public async Task<List<PaymentHistoryDto>> GetPaymentHistoryByClubAsync(int leaderId, int clubId)
        {
            // Kiểm tra leader có quyền với CLB này không
            if (!await _clubRepo.IsLeaderOfClubAsync(clubId, leaderId))
                throw new UnauthorizedAccessException("Bạn không phải leader của CLB này.");

            var paymentHistory = await _paymentRepo.GetPaymentHistoryByClubIdAsync(clubId);

            return paymentHistory.Select(p => new PaymentHistoryDto
            {
                Id = p.Id,
                MembershipId = p.MembershipId,
                ClubId = p.ClubId,
                ClubName = p.Club?.Name ?? "",
                Amount = p.Amount,
                PaidDate = p.PaidDate,
                Method = p.Method ?? "",
                Status = p.Status ?? "",
                Description = p.Description ?? "",
                AccountId = p.Membership?.AccountId ?? 0,
                FullName = p.Membership?.Account?.FullName ?? "",
                Email = p.Membership?.Account?.Email ?? ""
            }).ToList();
        }
    }
}


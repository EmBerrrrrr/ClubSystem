using DTO.DTO.Payment;
using Repository.Repo.Interfaces;
using Service.Helper;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    /// <summary>
    /// Service xử lý payment cho Club Leader: Xem payment, debtor (người nợ phí), history.
    /// 
    /// Công dụng: Giúp leader theo dõi tình hình thu phí thành viên trong CLB của mình.
    /// 
    /// Luồng chính từ front-end:
    /// 1. Front-end gọi API GET /api/leader/payment/club/{clubId} → Controller GetPaymentsByClub → Method GetPaymentsByClubAsync
    ///    → Kiểm tra quyền leader → Lấy từ DB bảng Payment (join Membership, Account, Club) → Trả DTO với status "paid"/"pending"/etc.
    /// 2. Tương tự cho GetDebtorsByClubAsync (pending payments) và GetPaymentHistoryByClubAsync (all status).
    /// 
    /// Tương tác giữa các API:
    /// - Phải approve MembershipRequest trước (API leader/approve) → Tạo Membership với status "pending_payment" nếu có phí → Tạo Payment pending.
    /// - Student thanh toán (API payment/create-link → PayOS webhook) → Update Payment status "paid" → Membership "active".
    /// - Leader gọi API này để xem history sau khi có payment.
    /// </summary>
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

        /// <summary>
        /// Lấy danh sách tất cả payment của một CLB (đã thanh toán).
        /// 
        /// API: GET /api/leader/payment/club/{clubId}/payments
        /// Luồng: Kiểm tra quyền leader → Lấy từ DB bảng Payment (Status="paid") → Join Membership/Account/Club để lấy info.
        /// </summary>
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

        /// <summary>
        /// Lấy danh sách thành viên còn nợ phí (pending payments).
        /// 
        /// API: GET /api/leader/payment/club/{clubId}/debtors
        /// Luồng: Tương tự, lấy Payment với Status="pending".
        /// </summary>
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
                PaymentCreatedDate = p.PaidDate.ToVietnamTime()
            }).ToList();
        }

        /// <summary>
        /// Lấy lịch sử tất cả payment của CLB (paid, pending, failed...).
        /// 
        /// API: GET /api/leader/payment/club/{clubId}/history
        /// Luồng: Lấy tất cả Payment của club, không lọc status.
        /// </summary>
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
                PaidDate = p.PaidDate.ToVietnamTime(),
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

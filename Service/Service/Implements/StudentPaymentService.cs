using DTO.DTO.Payment;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class StudentPaymentService : IStudentPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;

        public StudentPaymentService(IPaymentRepository paymentRepo)
        {
            _paymentRepo = paymentRepo;
        }

        // Xem các khoản đã đóng phí
        public async Task<List<StudentPaidPaymentDto>> GetPaidPaymentsAsync(int accountId)
        {
            var payments = await _paymentRepo.GetPaidPaymentsByAccountIdAsync(accountId);

            return payments.Select(p => new StudentPaidPaymentDto
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
                Description = p.Description ?? ""
            }).ToList();
        }

        // Xem các khoản còn nợ
        public async Task<List<StudentDebtDto>> GetDebtsAsync(int accountId)
        {
            var payments = await _paymentRepo.GetPendingPaymentsByAccountIdAsync(accountId);

            return payments.Select(p => new StudentDebtDto
            {
                Id = p.Id,
                MembershipId = p.MembershipId,
                ClubId = p.ClubId,
                ClubName = p.Club?.Name ?? "",
                Amount = p.Amount,
                PaidDate = p.PaidDate,
                Status = p.Status ?? "",
                OrderCode = p.OrderCode,
                Description = p.Description ?? "",
                CreatedDate = p.PaidDate // Có thể dùng PaidDate hoặc tạo field CreatedDate riêng
            }).ToList();
        }

        // Lịch sử thanh toán (tất cả trạng thái)
        public async Task<List<PaymentHistoryDto>> GetPaymentHistoryAsync(int accountId)
        {
            var payments = await _paymentRepo.GetPaymentHistoryByAccountIdAsync(accountId);

            return payments.Select(p => new PaymentHistoryDto
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


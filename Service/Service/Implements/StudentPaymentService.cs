using DTO.DTO.Payment;
using Repository.Repo.Interfaces;
using Service.Helper;
using Service.Service.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    /// <summary>
    /// Service dành cho sinh viên (student) để xem tình hình thanh toán phí membership của bản thân.
    /// 
    /// Công dụng:
    /// - Xem các khoản phí đã thanh toán thành công.
    /// - Xem các khoản phí còn nợ (pending).
    /// - Xem lịch sử toàn bộ thanh toán (paid, pending, failed...).
    /// 
    /// Luồng từ front-end:
    /// 1. Student gọi GET /api/student/payment/paid → GetPaidPaymentsAsync
    ///    → Lấy Payment có Status="paid" của accountId → Map thành StudentPaidPaymentDto.
    /// 2. Student gọi GET /api/student/payment/debts → GetDebtsAsync
    ///    → Lấy Payment có Status="pending" → Map thành StudentDebtDto (hiển thị khoản nợ).
    /// 3. Student gọi GET /api/student/payment/history → GetPaymentHistoryAsync
    ///    → Lấy tất cả Payment của accountId (mọi status) → Map thành PaymentHistoryDto.
    /// 
    /// Tương tác giữa các API:
    /// - Dữ liệu lấy từ bảng Payment, join Membership → Club để lấy tên CLB.
    /// - Khoản nợ được tạo khi leader approve MembershipRequest (nếu club có phí) → Payment pending.
    /// - Sau khi thanh toán thành công (PayOS webhook) → Payment "paid" → Membership "active".
    /// - Student dùng các API này để biết mình còn nợ CLB nào, đã đóng phí những CLB nào.
    /// </summary>
    public class StudentPaymentService : IStudentPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IAuthRepository _authRepo;

        public StudentPaymentService(IPaymentRepository paymentRepo, IAuthRepository authRepo)
        {
            _paymentRepo = paymentRepo;
            _authRepo = authRepo;
        }

        /// <summary>
        /// Xem danh sách các khoản phí đã thanh toán thành công.
        /// 
        /// API: GET /api/student/payment/paid
        /// Luồng: Lấy Payment có Status="paid" của accountId → Join Club để lấy tên CLB → Trả list DTO.
        /// </summary>
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

        /// <summary>
        /// Xem danh sách các khoản phí còn nợ (pending).
        /// 
        /// API: GET /api/student/payment/debts
        /// Luồng: Lấy Payment có Status="pending" → Hiển thị để student biết cần thanh toán những khoản nào.
        /// </summary>
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
                CreatedDate = p.PaidDate, // Có thể dùng PaidDate hoặc tạo field CreatedDate riêng
            }).ToList();
        }

        /// <summary>
        /// Xem lịch sử toàn bộ thanh toán (mọi trạng thái: paid, pending, failed...).
        /// 
        /// API: GET /api/student/payment/history
        /// Luồng: Lấy tất cả Payment của accountId → Bao gồm cả thông tin cá nhân từ Membership.
        /// </summary>
        // Lịch sử thanh toán (tất cả trạng thái)
        public async Task<List<PaymentHistoryDto>> GetPaymentHistoryAsync(int accountId)
        {
            var payments = await _paymentRepo.GetPaymentHistoryByAccountIdAsync(accountId);
            var result = new List<PaymentHistoryDto>();

            foreach (var p in payments)
            {
                // Lấy FullName và Email từ Account navigation property hoặc query riêng nếu null
                string? fullName = null;
                string? email = null;

                if (p.Account != null)
                {
                    fullName = p.Account.FullName;
                    email = p.Account.Email;
                }
                else if (p.Membership?.Account != null)
                {
                    fullName = p.Membership.Account.FullName;
                    email = p.Membership.Account.Email;
                }
                else if (p.AccountId > 0)
                {
                    // Nếu Account navigation null nhưng AccountId > 0, query Account riêng
                    var account = await _authRepo.GetAccountByIdAsync(p.AccountId);
                    if (account != null)
                    {
                        fullName = account.FullName;
                        email = account.Email;
                    }
                }

                result.Add(new PaymentHistoryDto
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
                    AccountId = p.AccountId,
                    FullName = fullName ?? "",
                    Email = email ?? ""
                });
            }

            return result;
        }
    }
}


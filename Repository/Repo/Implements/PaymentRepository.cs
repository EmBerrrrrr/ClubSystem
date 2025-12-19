using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    /// <summary>
    /// Repository xử lý thanh toán (Payment): CRUD và truy vấn theo account/club/orderCode.
    /// 
    /// Công dụng: Quản lý payment phí membership (pending/paid/failed).
    /// 
    /// Luồng dữ liệu:
    /// - AddAsync → AddAsync vào DbSet Payments.
    /// - GetByIdAsync → FindAsync theo Id.
    /// - GetByMembershipIdAsync → FirstOrDefault theo MembershipId, AsNoTracking.
    /// - GetByOrderCodeAsync → FirstOrDefault theo OrderCode (cho webhook).
    /// - GetPaymentsByClubIdAsync → Lấy paid theo ClubId, include Club/Membership/Account.
    /// - GetPendingPaymentsByClubIdAsync → Lấy pending theo ClubId, include tương tự.
    /// - GetPaymentHistoryByClubIdAsync → Lấy all theo ClubId, include tương tự.
    /// - GetPaidPaymentsByAccountIdAsync → Lấy paid theo AccountId (qua Membership), include Club/Membership/Account.
    /// - GetPendingPaymentsByAccountIdAsync → Lấy pending theo AccountId.
    /// - GetPaymentHistoryByAccountIdAsync → Lấy all theo AccountId.
    /// - UpdateAsync → Update + SaveChangesAsync.
    /// - SaveAsync → Commit thay đổi.
    /// 
    /// Tương tác giữa các API/service:
    /// - Tạo payment khi approve request có phí: AddAsync + SaveAsync (Status="pending").
    /// - Student thanh toán: GetByIdAsync → Create link PayOS → Webhook: GetByOrderCodeAsync → Update Status="paid" + UpdateAsync.
    /// - Leader xem payment: GetPaymentsByClubIdAsync / GetPendingPaymentsByClubIdAsync.
    /// - Student xem debts/history: GetPendingPaymentsByAccountIdAsync / GetPaymentHistoryByAccountIdAsync.
    /// - Admin monitoring revenue: GetTotalRevenueFromMembersByClubIdAsync (sum Amount của paid).
    /// </summary>
    public class PaymentRepository : IPaymentRepository
    {
        private readonly StudentClubManagementContext _db;

        public PaymentRepository(StudentClubManagementContext db)
        {
            _db = db;
        }

        // Tạo payment mới (chưa SaveChanges)
        public async Task AddAsync(Payment payment)
        {
            await _db.Payments.AddAsync(payment);
        }

        // Lấy payment theo Id (dùng trong PayOSService)
        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _db.Payments.FindAsync(id);
        }

        // Lấy payment theo MembershipId (dùng cho màn student)
        public async Task<Payment?> GetByMembershipIdAsync(int membershipId)
        {
            return await _db.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.MembershipId == membershipId);
        }

        // Lấy payment theo OrderCode (dùng cho webhook PayOS)
        public async Task<Payment?> GetByOrderCodeAsync(long orderCode)
        {
            return await _db.Payments
                .FirstOrDefaultAsync(p =>
                    p.OrderCode.HasValue && p.OrderCode.Value == orderCode);
        }

        // Lấy tất cả payments của một CLB (bao gồm cả thông tin Membership và Account)
        public async Task<List<Payment>> GetPaymentsByClubIdAsync(int clubId)
        {
            return await _db.Payments
                .Where(p => p.ClubId == clubId)
                .Include(p => p.Club)
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Account)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        // Lấy lịch sử thanh toán (tất cả các trạng thái thanh toán)
        public async Task<List<Payment>> GetPaymentHistoryByClubIdAsync(int clubId)
        {
            return await _db.Payments
                .Where(p => p.ClubId == clubId)
                .Include(p => p.Club)
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Account)
                .OrderByDescending(p => p.Id) // Sắp xếp theo Id giảm dần (mới nhất trước)
                .ToListAsync();
        }

        // Lấy các payment còn nợ (status = pending)
        public async Task<List<Payment>> GetPendingPaymentsByClubIdAsync(int clubId)
        {
            return await _db.Payments
                .Where(p => p.ClubId == clubId && p.Status.ToLower() == "pending")
                .Include(p => p.Club)
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Account)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        // Tính tổng revenue từ các member đã đóng phí (status = paid)
        public async Task<decimal> GetTotalRevenueFromMembersByClubIdAsync(int clubId)
        {
            return await _db.Payments
                .Where(p => p.ClubId == clubId && p.Status.ToLower() == "paid")
                .SumAsync(p => p.Amount);
        }

        // ========== STUDENT PAYMENT METHODS ==========

        // Lấy tất cả payments của một student (accountId)
        public async Task<List<Payment>> GetPaymentsByAccountIdAsync(int accountId)
        {
            return await _db.Payments
                .Where(p => p.Membership.AccountId == accountId)
                .Include(p => p.Club)
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Account)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        // Lấy các payments đã đóng (status = paid) của student
        public async Task<List<Payment>> GetPaidPaymentsByAccountIdAsync(int accountId)
        {
            return await _db.Payments
                .Where(p => p.Membership.AccountId == accountId && p.Status.ToLower() == "paid")
                .Include(p => p.Club)
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Account)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        // Lấy các payments còn nợ (status = pending) của student
        public async Task<List<Payment>> GetPendingPaymentsByAccountIdAsync(int accountId)
        {
            return await _db.Payments
                .Where(p => p.Membership.AccountId == accountId && p.Status.ToLower() == "pending")
                .Include(p => p.Club)
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Account)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        // Lấy lịch sử thanh toán (tất cả trạng thái) của student
        public async Task<List<Payment>> GetPaymentHistoryByAccountIdAsync(int accountId)
        {
            return await _db.Payments
                .Where(p => p.Membership.AccountId == accountId)
                .Include(p => p.Club)
                .Include(p => p.Membership)
                    .ThenInclude(m => m.Account)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
        }

        // Cập nhật payment + lưu luôn
        public async Task UpdateAsync(Payment payment)
        {
            _db.Payments.Update(payment);
            await _db.SaveChangesAsync();
        }

        // Lưu tất cả thay đổi (dùng cho AddAsync)
        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ExistsOrderCodeAsync(int code)
        {
            return await _db.Payments.AnyAsync(p => p.OrderCode == code);
        }

    }
}

using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
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
    }
}

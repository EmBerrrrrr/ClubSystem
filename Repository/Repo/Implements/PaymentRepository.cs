using System.Threading.Tasks;
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

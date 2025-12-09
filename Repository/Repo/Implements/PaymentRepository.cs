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

        public async Task<Payment?> GetByIdAsync(int id)
        {
            return await _db.Payments
                .Include(p => p.Club)
                .Include(p => p.Membership)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Payment>> GetPaymentsByAccountIdAsync(int accountId)
        {
            return await _db.Payments
                .Include(p => p.Club)
                .Include(p => p.Membership)
                .Where(p => p.Membership != null && p.Membership.AccountId == accountId)
                .OrderByDescending(p => p.PaidDate)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByMembershipRequestIdAsync(int membershipRequestId)
        {
            // Lấy membership request để có accountId và clubId
            var request = await _db.MembershipRequests.FirstOrDefaultAsync(mr => mr.Id == membershipRequestId);
            if (request == null) return null;

            // Tìm payment thông qua membership với accountId và clubId tương ứng
            return await _db.Payments
                .Include(p => p.Club)
                .Include(p => p.Membership)
                .Where(p => p.Membership != null && 
                           p.Membership.AccountId == request.AccountId && 
                           p.ClubId == request.ClubId &&
                           p.Status == "pending")
                .FirstOrDefaultAsync();
        }

        public async Task AddPaymentAsync(Payment payment)
        {
            await _db.Payments.AddAsync(payment);
        }

        public async Task UpdatePaymentAsync(Payment payment)
        {
            _db.Payments.Update(payment);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}


using Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Repo.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);

        Task<Payment?> GetByIdAsync(int id); 

        Task<Payment?> GetByMembershipIdAsync(int membershipId);

        Task<Payment?> GetByOrderCodeAsync(long orderCode);

        Task<List<Payment>> GetPaymentsByClubIdAsync(int clubId);

        Task<List<Payment>> GetPaymentHistoryByClubIdAsync(int clubId);

        Task<List<Payment>> GetPendingPaymentsByClubIdAsync(int clubId);

        Task<decimal> GetTotalRevenueFromMembersByClubIdAsync(int clubId);

        // Student payment methods
        Task<List<Payment>> GetPaymentsByAccountIdAsync(int accountId);
        Task<List<Payment>> GetPaidPaymentsByAccountIdAsync(int accountId);
        Task<List<Payment>> GetPendingPaymentsByAccountIdAsync(int accountId);
        Task<List<Payment>> GetPaymentHistoryByAccountIdAsync(int accountId);

        Task UpdateAsync(Payment payment);

        Task SaveAsync();
        Task<bool> ExistsOrderCodeAsync(int code);
    }
}

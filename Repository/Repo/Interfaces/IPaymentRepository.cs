using Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Repo.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(int id);
        Task<List<Payment>> GetPaymentsByAccountIdAsync(int accountId);
        Task<List<Payment>> GetPaymentsByClubIdAsync(int clubId);
        Task<Payment?> GetPaymentByMembershipRequestIdAsync(int membershipRequestId);
        Task AddPaymentAsync(Payment payment);
        Task UpdatePaymentAsync(Payment payment);
        Task SaveAsync();
    }
}


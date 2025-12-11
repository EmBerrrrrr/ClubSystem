using Repository.Models;
using System.Threading.Tasks;

namespace Repository.Repo.Interfaces
{
    public interface IPaymentRepository
    {
        Task AddAsync(Payment payment);

        Task<Payment?> GetByIdAsync(int id); 

        Task<Payment?> GetByMembershipIdAsync(int membershipId);

        Task<Payment?> GetByOrderCodeAsync(long orderCode);

        Task UpdateAsync(Payment payment);

        Task SaveAsync();
    }
}

using Repository.Models;

namespace Repository.Repo.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment?> GetByIdAsync(int id);
        Task<Payment?> GetByOrderCodeAsync(long orderCode);
        Task UpdateAsync(Payment payment);
    }
}

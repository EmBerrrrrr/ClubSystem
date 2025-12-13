using DTO.DTO.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IClubLeaderPaymentService
    {
        Task<List<PaymentDto>> GetPaymentsByClubAsync(int leaderId, int clubId);
        Task<List<DebtorDto>> GetDebtorsByClubAsync(int leaderId, int clubId);
        Task<List<PaymentHistoryDto>> GetPaymentHistoryByClubAsync(int leaderId, int clubId);
    }
}


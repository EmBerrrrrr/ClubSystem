using DTO.DTO.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IPaymentService
    {
        Task<List<PaymentDto>> GetMyPendingPaymentsAsync(int accountId);
        Task<VNPayPaymentResponseDto> CreateVNPayPaymentAsync(int accountId, int membershipRequestId);
        Task<bool> ProcessVNPayCallbackAsync(Dictionary<string, string> vnpayData);
        Task<PaymentDto> CompletePaymentAsync(int accountId, int paymentId);
        Task<List<PaymentDto>> GetMyPaymentHistoryAsync(int accountId);
        Task<List<PaymentStatusDto>> GetMyPaymentStatusAsync(int accountId);
    }
}


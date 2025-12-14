using DTO.DTO.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IStudentPaymentService
    {
        // Xem các khoản đã đóng phí
        Task<List<StudentPaidPaymentDto>> GetPaidPaymentsAsync(int accountId);

        // Xem các khoản còn nợ
        Task<List<StudentDebtDto>> GetDebtsAsync(int accountId);

        // Lịch sử thanh toán (tất cả trạng thái)
        Task<List<PaymentHistoryDto>> GetPaymentHistoryAsync(int accountId);
    }
}


using DTO.DTO.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    /// <summary>
    /// Service để Club Leader quản lý payment của CLB
    /// </summary>
    public interface IClubLeaderPaymentService
    {
        /// <summary>
        /// Xem danh sách tất cả payments của CLB
        /// </summary>
        Task<List<ClubPaymentDto>> GetClubPaymentsAsync(int leaderId, int clubId);

        /// <summary>
        /// Xem lịch sử thanh toán của một member cụ thể trong CLB
        /// </summary>
        Task<List<MemberPaymentDto>> GetMemberPaymentsAsync(int leaderId, int clubId, int accountId);

        /// <summary>
        /// Kiểm tra danh sách members còn nợ phí CLB
        /// </summary>
        Task<List<DebtMemberDto>> GetDebtMembersAsync(int leaderId, int clubId);
    }
}


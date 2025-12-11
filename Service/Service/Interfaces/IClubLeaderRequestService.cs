using Service.DTO.ClubLeader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IClubLeaderRequestService
    {
        Task CreateRequestAsync(int accountId, string reason);
        Task<List<LeaderRequestDto>> GetPendingAsync();
        Task<List<ProcessedLeaderRequestDto>> GetApprovedAsync();
        Task<List<ProcessedLeaderRequestDto>> GetRejectedAsync();
        Task<LeaderRequestStatsDto> GetStatsAsync();
        Task ApproveAsync(int requestId, int adminId, string? note = null);
        Task RejectAsync(int requestId, int adminId, string reason);
        Task<MyLeaderRequestDto?> GetMyRequestAsync(int accountId);

    }
}

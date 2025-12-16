using DTO.DTO.ClubLeader;
using Service.DTO.ClubLeader;

namespace Service.Service.Interfaces
{
    public interface IClubLeaderRequestService
    {
        Task CreateRequestAsync(int accountId, CreateLeaderRequestDto dto);
        Task<MyLeaderRequestDto?> GetMyRequestAsync(int accountId);
        Task<List<LeaderRequestDto>> GetPendingAsync();
        Task<List<ProcessedLeaderRequestDto>> GetApprovedAsync();
        Task<List<ProcessedLeaderRequestDto>> GetRejectedAsync();
        Task<LeaderRequestStatsDto> GetStatsAsync();
        Task ApproveAsync(int requestId, int adminId, string? adminNote);
        Task RejectAsync(int requestId, int adminId, string rejectReason);
        Task<ProcessedLeaderRequestDto?> GetRequestDetailAsync(int id);
    }
}

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
        Task ApproveAsync(int requestId, int adminId);
        Task RejectAsync(int requestId, int adminId, string reason);
    }
}

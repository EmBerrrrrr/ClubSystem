using Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Repo.Interfaces
{
    public interface IMembershipRequestRepository
    {
        Task<bool> HasPendingRequestAsync(int accountId, int clubId);
        Task CreateRequestAsync(MembershipRequest request);
        Task<List<MembershipRequest>> GetRequestsOfAccountAsync(int accountId);
        Task SaveAsync();
        Task<MembershipRequest?> GetByIdAsync(int id);
        Task<List<MembershipRequest>> GetPendingRequestsByClubAsync(int clubId);
        Task<List<MembershipRequest>> GetAllRequestsByClubAsync(int clubId);
        Task UpdateAsync(MembershipRequest req);

    }
}

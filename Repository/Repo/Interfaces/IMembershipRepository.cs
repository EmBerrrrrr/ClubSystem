using Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Repo.Interfaces
{
    public interface IMembershipRepository
    {
        Task<bool> IsMemberAsync(int accountId, int clubId);
        Task<List<Membership>> GetMembershipsAsync(int accountId);
        Task<Membership?> GetMembershipByAccountAndClubAsync(int accountId, int clubId);
        Task AddMembershipAsync(Membership member);
        Task SaveAsync();
    }
}

using Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repository.Repo.Interfaces
{
    public interface IMembershipRepository
    {
        Task<bool> IsMemberAsync(int accountId, int clubId);
        Task<List<Membership>> GetMembershipsAsync(int accountId);
        Task<List<Membership>> GetAllMembershipsAsync(int accountId); // Lấy tất cả bao gồm pending_payment
        Task<List<Membership>> GetMembershipsByClubIdAsync(int clubId); // Lấy tất cả members của một CLB
        Task<int> GetActiveMemberCountByClubIdAsync(int clubId); // Đếm số thành viên active của một CLB
        Task<Dictionary<int, int>> GetActiveMemberCountsByClubIdsAsync(List<int> clubIds);
        Task<Membership?> GetMembershipByAccountAndClubAsync(int accountId, int clubId);
        Task<Membership?> GetMembershipByIdAsync(int membershipId);
        Task AddMembershipAsync(Membership member);
        void UpdateMembership(Membership membership);
        Task SaveAsync();
    }
}

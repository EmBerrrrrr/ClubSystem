using DTO.DTO.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IClubLeaderMembershipService
    {
        Task<List<MembershipRequestForLeaderDto>> GetPendingRequestsAsync(int leaderId);
        Task<List<ClubMemberDto>> GetClubMembersAsync(int leaderId);
        Task<List<ClubMemberDto>> GetClubMembersByClubIdAsync(int leaderId, int clubId);
        Task ApproveAsync(int leaderId, int requestId, string? note);
        Task RejectAsync(int leaderId, int requestId, string? note);
        Task LockMemberAsync(int leaderId, int membershipId, string? reason);
        Task UnlockMemberAsync(int leaderId, int membershipId);
        Task RemoveMemberAsync(int leaderId, int membershipId, string? reason);
    }

}

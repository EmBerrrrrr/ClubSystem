using DTO;
using DTO.DTO.Activity;
using DTO.DTO.Club;
using DTO.DTO.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface ILeaderClubService
    {
        Task<List<LeaderClubDTO>> GetClubsOfLeaderAsync(int leaderAccountId);

        Task<LeaderClubDTO?> GetClubOfLeaderAsync(int leaderAccountId, int clubId);

        Task<LeaderClubDTO> CreateClubAsync(int leaderAccountId, LeaderClubCreateDTO dto);

        Task<bool> UpdateClubAsync(int leaderAccountId, int clubId, LeaderClubUpdateDTO dto);

        Task<bool> DeleteClubAsync(int leaderAccountId, int clubId);

        Task<List<MembershipRequestDTO>> GetMembershipRequestsAsync(
        int leaderAccountId, int clubId, string? status);

        Task<bool> ApproveMembershipRequestAsync(
            int leaderAccountId, int clubId, int requestId);

        Task<bool> RejectMembershipRequestAsync(
            int leaderAccountId, int clubId, int requestId, string reason);

        Task<List<MembershipDTO>> GetMembersAsync(int leaderAccountId, int clubId);

        Task<bool> UpdateMemberStatusAsync(
            int leaderAccountId, int clubId, int membershipId, string status);

        Task<bool> RemoveMemberAsync(
            int leaderAccountId, int clubId, int membershipId);

        Task<List<ActivityDTO>> GetActivitiesAsync(int leaderAccountId, int clubId);
        Task<ActivityDTO?> CreateActivityAsync(int leaderAccountId, int clubId, ActivityCreateDTO dto);
        Task<bool> UpdateActivityAsync(int leaderAccountId, int clubId, int activityId, ActivityUpdateDTO dto);
        Task<bool> DeleteActivityAsync(int leaderAccountId, int clubId, int activityId);

    }

}

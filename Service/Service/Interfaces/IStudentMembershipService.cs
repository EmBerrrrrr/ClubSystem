using DTO.DTO.Membership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IStudentMembershipService
    {
        Task<AccountInfoDto> GetAccountInfoAsync(int accountId);
        Task SendMembershipRequestAsync(int accountId, CreateMembershipRequestDto dto);
        Task<List<MembershipRequestDto>> GetMyRequestsAsync(int accountId);
        Task<MembershipRequestDto> GetRequestDetailAsync(int requestId, int accountId);
        Task<List<MyMembershipDto>> GetMyMembershipsAsync(int accountId);
    }

}

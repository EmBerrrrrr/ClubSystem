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
        Task SendMembershipRequestAsync(int accountId, int clubId);
        Task<List<MembershipRequestDto>> GetMyRequestsAsync(int accountId);
        Task<List<MyMembershipDto>> GetMyMembershipsAsync(int accountId);
    }

}

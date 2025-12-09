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
        Task<List<MembershipRequestDTO>> GetMyRequestsAsync(int accountId);
        Task<List<MembershipDTO>> GetMyClubsAsync(int accountId);
    }
}

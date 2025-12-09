using DTO.DTO.Membership;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class StudentMembershipService : IStudentMembershipService
    {
        private readonly StudentClubManagementContext _context;

        public StudentMembershipService(StudentClubManagementContext context)
        {
            _context = context;
        }

        // xem tất cả đơn mình đã gửi
        public async Task<List<MembershipRequestDTO>> GetMyRequestsAsync(int accountId)
        {
            return await _context.MembershipRequests
                .Where(r => r.AccountId == accountId)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new MembershipRequestDTO
                {
                    Id = r.Id,
                    AccountId = r.AccountId,
                    AccountName = r.Account.FullName,
                    RequestDate = r.RequestDate,
                    Status = r.Status!
                })
                .ToListAsync();
        }

        // xem CLB mình đang là member
        public async Task<List<MembershipDTO>> GetMyClubsAsync(int accountId)
        {
            return await _context.Memberships
                .Where(m => m.AccountId == accountId && m.Status == "active")
                .Select(m => new MembershipDTO
                {
                    Id = m.Id,
                    AccountId = m.AccountId,
                    AccountName = m.Account.FullName,
                    JoinDate = m.JoinDate.HasValue ? m.JoinDate.Value.ToDateTime(System.TimeOnly.MinValue) : (DateTime?)null,
                    Status = m.Status!,
                    // có thể thêm Name club nếu muốn:
                    // ClubName   = m.Club.Name
                })
                .ToListAsync();
        }
    }

}

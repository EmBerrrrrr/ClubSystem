using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class ClubLeaderRequestService : IClubLeaderRequestService
    {
        private readonly StudentClubManagementContext _db;
        private readonly IClubLeaderRequestRepository _repo;

        public ClubLeaderRequestService(StudentClubManagementContext db,
                                         IClubLeaderRequestRepository repo)
        {
            _db = db;
            _repo = repo;
        }

        // Student gửi request
        public async Task CreateRequestAsync(int accountId)
        {
            var exists = await _db.ClubLeaderRequests
                .AnyAsync(x =>
                    x.AccountId == accountId &&
                    x.Status == "Pending");
            if (exists)
                throw new Exception("Bạn đã gửi request và đang chờ duyệt");
            var request = new ClubLeaderRequest
            {
                AccountId = accountId,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            };
            await _repo.CreateAsync(request);
            await _repo.SaveAsync();
        }


        // Admin xem list
        public async Task<List<LeaderRequestDto>> GetPendingAsync()
        {
            var requests = await _repo.GetPendingAsync();

            return requests.Select(x => new LeaderRequestDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                RequestDate = x.RequestDate,
                Status = x.Status,
                Note = x.Note
            }).ToList();
        }

        // APPROVE
        public async Task ApproveAsync(int requestId, int adminId)
        {
            var request = await _repo.GetByIdAsync(requestId);
            if (request == null)
                throw new Exception("Request không tồn tại");

            request.Status = "APPROVED";
            request.ProcessedBy = adminId;
            request.ProcessedAt = DateTime.UtcNow;
            request.Note = "Approved";

            var leaderRole = await _db.Roles
                .FirstOrDefaultAsync(x => x.Name == "CLUB_LEADER");

            if (leaderRole == null)
            {
                leaderRole = new Role
                {
                    Name = "CLUB_LEADER",
                    Description = "Club leader role"
                };

                _db.Roles.Add(leaderRole);
                await _db.SaveChangesAsync();
            }

            bool existsRole = await _db.AccountRoles.AnyAsync(x =>
                x.AccountId == request.AccountId &&
                x.RoleId == leaderRole.Id);

            if (!existsRole)
            {
                _db.AccountRoles.Add(new AccountRole
                {
                    AccountId = request.AccountId,
                    RoleId = leaderRole.Id
                });
            }

            await _repo.SaveAsync();
        }


        // REJECT
        public async Task RejectAsync(int requestId, int adminId, string reason)
        {
            var request = await _repo.GetByIdAsync(requestId);

            if (request == null)
                throw new Exception("Request không tồn tại");

            request.Status = "REJECTED";
            request.ProcessedBy = adminId;
            request.ProcessedAt = DateTime.UtcNow;
            request.Note = string.IsNullOrWhiteSpace(reason)
                ? "Rejected"
                : reason;

            await _repo.SaveAsync();
        }
    }

}

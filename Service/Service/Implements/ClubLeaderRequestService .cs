using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.DTO.ClubLeader;
using Service.Service.Interfaces;
using System;

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
        public async Task CreateRequestAsync(int accountId, string reason)
        {
            //1. Không cho clubleader gửi request
            bool isLeader = await _db.AccountRoles
                .Include(ar => ar.Role)
                .AnyAsync(ar =>
                    ar.AccountId == accountId &&
                    ar.Role.Name.ToLower() == "clubleader");

            if (isLeader)
                throw new Exception("Bạn đã là Club Leader, không thể gửi request nữa");

            //2. Không cho gửi nếu đã có request pending
            var exists = await _db.ClubLeaderRequests
                .AnyAsync(x =>
                    x.AccountId == accountId &&
                    x.Status.ToLower() == "pending");
            if (exists)
                throw new Exception("Bạn đã gửi request và đang chờ duyệt");

            var request = new ClubLeaderRequest
            {
                AccountId = accountId,
                RequestDate = DateTime.UtcNow,
                Status = "pending",
                Reason = string.IsNullOrWhiteSpace(reason)
                    ? "No reason provided"
                    : reason.Trim()
            };
            await _repo.CreateAsync(request);
            await _repo.SaveAsync();
        }

        // Student xem list của bản thân
        public async Task<MyLeaderRequestDto?> GetMyRequestAsync(int accountId)
        {
            var req = await _db.ClubLeaderRequests
                .Include(x => x.Account)
                .Where(x => x.AccountId == accountId)
                .OrderByDescending(x => x.RequestDate)
                .FirstOrDefaultAsync();

            if (req == null)
                return null;

            return new MyLeaderRequestDto
            {
                Id = req.Id,
                RequestDate = req.RequestDate,
                Status = req.Status,
                Reason = req.Reason,
                Note = req.Note,
                Phone = req.Account?.Phone
            };

        }

        // Admin xem list pending
        public async Task<List<LeaderRequestDto>> GetPendingAsync()
        {
            var requests = await _db.ClubLeaderRequests
                .Include(x => x.Account) // JOIN ACCOUNT
                .Where(x => x.Status.ToLower() == "pending")
                .OrderByDescending(x => x.RequestDate)
                .ToListAsync();

            return requests.Select(x => new LeaderRequestDto
            {
                Id = x.Id,
                AccountId = x.AccountId,

                Username = x.Account.Username,
                FullName = x.Account.FullName,
                Email = x.Account.Email,
                Phone = x.Account.Phone,

                RequestDate = x.RequestDate,
                Status = x.Status,
                Reason = x.Reason,
                Note = x.Note
            }).ToList();
        }

        // Admin xem list đã duyệt
        public async Task<List<ProcessedLeaderRequestDto>> GetApprovedAsync()
        {
            var requests = await _db.ClubLeaderRequests
                .Include(x => x.Account)
                .Include(x => x.ProcessedByNavigation)
                .Where(x => x.Status.ToLower() == "approved")
                .OrderByDescending(x => x.ProcessedAt)
                .ToListAsync();

            return requests.Select(x => new ProcessedLeaderRequestDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Username = x.Account?.Username ?? string.Empty,
                FullName = x.Account?.FullName,
                Email = x.Account?.Email,
                Phone = x.Account?.Phone,
                RequestDate = x.RequestDate,
                Status = x.Status,
                Reason = x.Reason,
                Note = x.Note,
                ProcessedBy = x.ProcessedBy,
                ProcessedByUsername = x.ProcessedByNavigation?.Username,
                ProcessedByFullName = x.ProcessedByNavigation?.FullName,
                ProcessedAt = x.ProcessedAt
            }).ToList();
        }

        // Admin xem list đã từ chối
        public async Task<List<ProcessedLeaderRequestDto>> GetRejectedAsync()
        {
            var requests = await _db.ClubLeaderRequests
                .Include(x => x.Account)
                .Include(x => x.ProcessedByNavigation)
                .Where(x => x.Status.ToLower() == "rejected")
                .OrderByDescending(x => x.ProcessedAt)
                .ToListAsync();

            return requests.Select(x => new ProcessedLeaderRequestDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Username = x.Account?.Username ?? string.Empty,
                FullName = x.Account?.FullName,
                Email = x.Account?.Email,
                Phone = x.Account?.Phone,
                RequestDate = x.RequestDate,
                Status = x.Status,
                Reason = x.Reason,
                Note = x.Note,
                ProcessedBy = x.ProcessedBy,
                ProcessedByUsername = x.ProcessedByNavigation?.Username,
                ProcessedByFullName = x.ProcessedByNavigation?.FullName,
                ProcessedAt = x.ProcessedAt
            }).ToList();
        }

        // Admin xem thống kê
        public async Task<LeaderRequestStatsDto> GetStatsAsync()
        {
            var totalApproved = await _db.ClubLeaderRequests
                .CountAsync(x => x.Status.ToLower() == "approved");
            
            var totalRejected = await _db.ClubLeaderRequests
                .CountAsync(x => x.Status.ToLower() == "rejected");
            
            var totalPending = await _db.ClubLeaderRequests
                .CountAsync(x => x.Status.ToLower() == "pending");
            
            var total = await _db.ClubLeaderRequests.CountAsync();

            return new LeaderRequestStatsDto
            {
                TotalApproved = totalApproved,
                TotalRejected = totalRejected,
                TotalPending = totalPending,
                Total = total
            };
        }

        // APPROVE
        public async Task ApproveAsync(int requestId, int adminId, string? note = null)
        {
            var request = await _repo.GetByIdAsync(requestId);
            if (request == null)
                throw new Exception("Request không tồn tại");

            if (request.Status.ToLower() != "pending")
                throw new Exception("Request đã được xử lý");

            //Không cho approve nếu user đã là clubleader
            bool alreadyLeader = await _db.AccountRoles
                .Include(ar => ar.Role)
                .AnyAsync(ar => ar.AccountId == request.AccountId &&
                                ar.Role.Name.ToLower() == "clubleader");

            if (alreadyLeader)
                throw new Exception("Tài khoản này đã là Club Leader");

            request.Status = "approved";
            request.ProcessedBy = adminId;
            request.ProcessedAt = DateTime.UtcNow;
            request.Note = string.IsNullOrWhiteSpace(note)
                ? "Đã duyệt"
                : note.Trim();

            var leaderRole = await _db.Roles
                .FirstOrDefaultAsync(x => x.Name == "clubleader");
            if (leaderRole == null)
            {
                leaderRole = new Role
                {
                    Name = "clubleader",
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
            
            if (request.Status.ToLower() != "pending")
                throw new Exception("Request đã được xử lý");
            
            request.Status = "rejected";
            request.ProcessedBy = adminId;
            request.ProcessedAt = DateTime.UtcNow;
            request.Note = string.IsNullOrWhiteSpace(reason)
                ? "Đã từ chối"
                : reason.Trim();
            await _repo.SaveAsync();
        }
    }

}

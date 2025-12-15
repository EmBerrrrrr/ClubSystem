using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.DTO.ClubLeader;
using Service.Service.Interfaces;
using Service.Helper;
using System;

namespace Service.Service.Implements
{
    public class ClubLeaderRequestService : IClubLeaderRequestService
    {
        private readonly StudentClubManagementContext _db;
        private readonly IClubLeaderRequestRepository _repo;
        private readonly INotificationService _noti;
        public ClubLeaderRequestService(
            StudentClubManagementContext db,
            IClubLeaderRequestRepository repo,
            INotificationService noti)
        {
            _db = db;
            _repo = repo;
            _noti = noti;
        }

        // Student gửi request
        public async Task CreateRequestAsync(int accountId, string reason)
        {
            bool isLeader = await _db.AccountRoles
                .Include(x => x.Role)
                .AnyAsync(x => x.AccountId == accountId &&
                               x.Role.Name.ToLower() == "clubleader");

            if (isLeader)
                throw new Exception("Bạn đã là Club Leader");

            bool exists = await _db.ClubLeaderRequests
                .AnyAsync(x => x.AccountId == accountId &&
                               x.Status.ToLower() == "pending");

            if (exists)
                throw new Exception("Bạn đã gửi request và đang chờ duyệt");

            var request = new ClubLeaderRequest
            {
                AccountId = accountId,
                RequestDate = DateTimeExtensions.NowVietnam(),
                Status = "pending",
                Reason = string.IsNullOrWhiteSpace(reason) ? "No reason" : reason.Trim()
            };

            await _repo.CreateAsync(request);
            await _repo.SaveAsync();

            // 🔔 NOTI → ADMIN
            var adminIds = await _db.AccountRoles
                .Include(x => x.Role)
                .Where(x => x.Role.Name == "admin")
                .Select(x => x.AccountId)
                .ToListAsync();

            foreach (var adminId in adminIds)
            {
                _noti.Push(
                    adminId,
                    "Yêu cầu Club Leader mới",
                    "Có sinh viên gửi yêu cầu trở thành Club Leader"
                );
            }
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
            var request = await _repo.GetByIdAsync(requestId)
                ?? throw new Exception("Request không tồn tại");

            if (request.Status.ToLower() != "pending")
                throw new Exception("Request đã xử lý");

            request.Status = "approved";
            request.ProcessedBy = adminId;
            request.ProcessedAt = DateTimeExtensions.NowVietnam();
            request.Note = note ?? "Đã duyệt";

            var role = await _db.Roles.FirstAsync(x => x.Name == "clubleader");

            if (!await _db.AccountRoles.AnyAsync(x =>
                    x.AccountId == request.AccountId &&
                    x.RoleId == role.Id))
            {
                _db.AccountRoles.Add(new AccountRole
                {
                    AccountId = request.AccountId,
                    RoleId = role.Id
                });
            }

            await _repo.SaveAsync();

            // 🔔 NOTI → STUDENT
            _noti.Push(
                request.AccountId,
                "Yêu cầu được duyệt 🎉",
                "Bạn đã trở thành Club Leader"
            );
        }

        // REJECT
        public async Task RejectAsync(int requestId, int adminId, string reason)
        {
            var request = await _repo.GetByIdAsync(requestId)
                ?? throw new Exception("Request không tồn tại");

            request.Status = "rejected";
            request.ProcessedBy = adminId;
            request.ProcessedAt = DateTimeExtensions.NowVietnam();
            request.Note = reason;

            await _repo.SaveAsync();

            // 🔔 NOTI → STUDENT
            _noti.Push(
                request.AccountId,
                "Yêu cầu bị từ chối",
                reason
            );
        }

        public async Task<ProcessedLeaderRequestDto?> GetRequestDetailAsync(int id)
        {
            var x = await _db.ClubLeaderRequests
                .Include(r => r.Account)
                .Include(r => r.ProcessedByNavigation)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (x == null) return null;

            return new ProcessedLeaderRequestDto
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
            };
        }

    }
}

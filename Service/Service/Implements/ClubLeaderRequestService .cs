using DTO.DTO.ClubLeader;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.DTO.ClubLeader;
using Service.Helper;
using Service.Service.Interfaces;

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

        // ================= STUDENT CREATE =================
        public async Task CreateRequestAsync(int accountId, CreateLeaderRequestDto dto)
        {
            bool isLeader = await _db.AccountRoles
                .Include(x => x.Role)
                .AnyAsync(x => x.AccountId == accountId &&
                               x.Role.Name.ToLower() == "clubleader");

            if (isLeader)
                throw new Exception("Bạn đã là Club Leader");

            bool exists = await _db.ClubLeaderRequests
                .AnyAsync(x => x.AccountId == accountId &&
                               x.Status == "pending");

            if (exists)
                throw new Exception("Bạn đã gửi request và đang chờ duyệt");

            var request = new ClubLeaderRequest
            {
                AccountId = accountId,
                RequestDate = DateTimeExtensions.NowVietnam(),
                Status = "pending",

                Motivation = dto.Motivation,
                Experience = dto.Experience,
                Vision = dto.Vision,
                Commitment = dto.Commitment
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

        // ================= STUDENT VIEW =================
        public async Task<MyLeaderRequestDto?> GetMyRequestAsync(int accountId)
        {
            var req = await _db.ClubLeaderRequests
                .Include(x => x.Account)
                .Where(x => x.AccountId == accountId)
                .OrderByDescending(x => x.RequestDate)
                .FirstOrDefaultAsync();

            if (req == null) return null;

            return new MyLeaderRequestDto
            {
                Id = req.Id,
                RequestDate = DateTimeExtensions.NowVietnam(),
                Username = req.Account.Username,
                FullName = req.Account.FullName,
                Email = req.Account.Email,
                Phone = req.Account.Phone,
                Status = req.Status,
                AdminNote = req.AdminNote,
                RejectReason = req.RejectReason,
                Motivation = req.Motivation,
                Experience = req.Experience,
                Vision = req.Vision,
                Commitment = req.Commitment
            };
        }

        // ================= ADMIN PENDING =================
        public async Task<List<LeaderRequestDto>> GetPendingAsync()
        {
            var requests = await _db.ClubLeaderRequests
                .Include(x => x.Account)
                .Where(x => x.Status == "pending")
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
                RequestDate = DateTimeExtensions.NowVietnam(),
                Status = x.Status,
                Motivation = x.Motivation,
                Experience = x.Experience,
                Vision = x.Vision,
                Commitment = x.Commitment
            }).ToList();
        }

        // ================= ADMIN APPROVED =================
        public async Task<List<ProcessedLeaderRequestDto>> GetApprovedAsync()
        {
            var requests = await _db.ClubLeaderRequests
                .Include(x => x.Account)
                .Include(x => x.ProcessedByNavigation)
                .Where(x => x.Status == "approved")
                .OrderByDescending(x => x.ProcessedAt)
                .ToListAsync();

            return requests.Select(x => MapProcessed(x)).ToList();
        }

        // ================= ADMIN REJECTED =================
        public async Task<List<ProcessedLeaderRequestDto>> GetRejectedAsync()
        {
            var requests = await _db.ClubLeaderRequests
                .Include(x => x.Account)
                .Include(x => x.ProcessedByNavigation)
                .Where(x => x.Status == "rejected")
                .OrderByDescending(x => x.ProcessedAt)
                .ToListAsync();

            return requests.Select(x => MapProcessed(x)).ToList();
        }

        // ================= STATS =================
        public async Task<LeaderRequestStatsDto> GetStatsAsync()
        {
            return new LeaderRequestStatsDto
            {
                TotalApproved = await _db.ClubLeaderRequests.CountAsync(x => x.Status == "approved"),
                TotalRejected = await _db.ClubLeaderRequests.CountAsync(x => x.Status == "rejected"),
                TotalPending = await _db.ClubLeaderRequests.CountAsync(x => x.Status == "pending"),
                Total = await _db.ClubLeaderRequests.CountAsync()
            };
        }

        // ================= APPROVE =================
        public async Task ApproveAsync(int requestId, int adminId, string? adminNote)
        {
            var request = await _repo.GetByIdAsync(requestId)
                ?? throw new Exception("Request không tồn tại");

            if (request.Status != "pending")
                throw new Exception("Request đã xử lý");

            request.Status = "approved";
            request.AdminNote = adminNote;
            request.ProcessedBy = adminId;
            request.ProcessedAt = DateTime.UtcNow;

            var role = await _db.Roles.FirstAsync(x => x.Name == "clubleader");

            if (!await _db.AccountRoles.AnyAsync(x =>
                x.AccountId == request.AccountId && x.RoleId == role.Id))
            {
                _db.AccountRoles.Add(new AccountRole
                {
                    AccountId = request.AccountId,
                    RoleId = role.Id
                });
            }

            await _repo.SaveAsync();

            _noti.Push(
                request.AccountId,
                "Yêu cầu được duyệt 🎉",
                "Bạn đã trở thành Club Leader"
            );
        }

        // ================= REJECT =================
        public async Task RejectAsync(int requestId, int adminId, string rejectReason)
        {
            var request = await _repo.GetByIdAsync(requestId)
                ?? throw new Exception("Request không tồn tại");

            request.Status = "rejected";
            request.RejectReason = rejectReason;
            request.ProcessedBy = adminId;
            request.ProcessedAt = DateTime.UtcNow;

            await _repo.SaveAsync();

            _noti.Push(
                request.AccountId,
                "Yêu cầu bị từ chối",
                rejectReason
            );
        }

        // ================= DETAIL =================
        public async Task<ProcessedLeaderRequestDto?> GetRequestDetailAsync(int id)
        {
            var x = await _db.ClubLeaderRequests
                .Include(r => r.Account)
                .Include(r => r.ProcessedByNavigation)
                .FirstOrDefaultAsync(r => r.Id == id);

            return x == null ? null : MapProcessed(x);
        }

        private static ProcessedLeaderRequestDto MapProcessed(ClubLeaderRequest x)
        {
            return new ProcessedLeaderRequestDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                Username = x.Account?.Username ?? "",
                FullName = x.Account?.FullName,
                Email = x.Account?.Email,
                Phone = x.Account?.Phone,
                RequestDate = DateTimeExtensions.NowVietnam(),
                Status = x.Status,
                AdminNote = x.AdminNote,
                RejectReason = x.RejectReason,
                ProcessedBy = x.ProcessedBy,
                ProcessedByUsername = x.ProcessedByNavigation?.Username,
                ProcessedByFullName = x.ProcessedByNavigation?.FullName,
                ProcessedAt = x.ProcessedAt
            };
        }
    }
}

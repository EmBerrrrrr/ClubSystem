using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    /// <summary>
    /// Repository xử lý yêu cầu tham gia CLB (MembershipRequest).
    /// 
    /// Công dụng: Truy vấn và quản lý request từ student muốn join CLB.
    /// 
    /// Luồng dữ liệu:
    /// - HasPendingRequestAsync → Check tồn tại request pending theo AccountId/ClubId.
    /// - CreateRequestAsync → Add vào DbSet MembershipRequests.
    /// - GetRequestsOfAccountAsync → Lấy list của accountId, include Club/Account, sort descending RequestDate.
    /// - SaveAsync/UpdateAsync → Commit thay đổi.
    /// - GetByIdAsync → FirstOrDefault theo Id.
    /// - GetPendingRequestsByClubAsync/GetAllRequestsByClubAsync → Lấy theo ClubId, include Account, lọc Status="Pending" hoặc all.
    /// 
    /// Tương tác giữa các API/service:
    /// - Student gửi request (API /membership/request): HasPendingRequestAsync (check) → CreateRequestAsync + SaveAsync.
    /// - Student xem my requests (API /membership/requests): GetRequestsOfAccountAsync.
    /// - Leader xem pending requests (API /leader/membership/pending): GetPendingRequestsByClubAsync.
    /// - Approve/reject: GetByIdAsync → Update Status/ProcessedBy/Note → UpdateAsync.
    /// </summary>
    public class MembershipRequestRepository : IMembershipRequestRepository
    {
        private readonly StudentClubManagementContext _db;

        public MembershipRequestRepository(StudentClubManagementContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Kiểm tra có request pending cho account/club.
        /// 
        /// Luồng: AnyAsync trên MembershipRequests → Lọc AccountId/ClubId/Status="Pending".
        /// </summary>
        public async Task<bool> HasPendingRequestAsync(int accountId, int clubId)
        {
            return await _db.MembershipRequests
                .AnyAsync(x =>
                    x.AccountId == accountId &&
                    x.ClubId == clubId &&
                    x.Status != null && x.Status.ToLower() == "pending");
        }

        /// <summary>
        /// Thêm request mới (chưa save).
        /// 
        /// Luồng: AddAsync vào DbSet MembershipRequests.
        /// </summary>
        public async Task CreateRequestAsync(MembershipRequest request)
        {
            await _db.MembershipRequests.AddAsync(request);
        }

        /// <summary>
        /// Lấy list request của account.
        /// 
        /// Luồng: Query → Lọc AccountId → Include Club/Account → Sort descending RequestDate.
        /// </summary>
        public async Task<List<MembershipRequest>> GetRequestsOfAccountAsync(int accountId)
        {
            return await _db.MembershipRequests
                .Where(x => x.AccountId == accountId)
                .Include(x => x.Club)
                .Include(x => x.Account)
                .OrderByDescending(x => x.RequestDate)
                .ToListAsync();
        }

        /// <summary>
        /// Lưu thay đổi (commit DB).
        /// 
        /// Luồng: SaveChangesAsync sau Add/Update.
        /// </summary>
        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy request theo Id.
        /// 
        /// Luồng: FirstOrDefaultAsync theo Id.
        /// </summary>
        public async Task<MembershipRequest?> GetByIdAsync(int id)
        {
            return await _db.MembershipRequests.FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <summary>
        /// Lấy list request pending theo club.
        /// 
        /// Luồng: Query → Lọc ClubId/Status="Pending" → Include Account.
        /// </summary>
        public async Task<List<MembershipRequest>> GetPendingRequestsByClubAsync(int clubId)
        {
            return await _db.MembershipRequests
                .Where(r => r.ClubId == clubId && r.Status != null && r.Status.ToLower() == "pending")
                .Include(r => r.Account)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy list tất cả request theo club.
        /// 
        /// Luồng: Query → Lọc ClubId → Include Account → Sort descending RequestDate.
        /// </summary>
        public async Task<List<MembershipRequest>> GetAllRequestsByClubAsync(int clubId)
        {
            return await _db.MembershipRequests
                .Where(r => r.ClubId == clubId)
                .Include(r => r.Account)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        /// <summary>
        /// Cập nhật request (save luôn).
        /// 
        /// Luồng: Update → SaveChangesAsync.
        /// </summary>
        public async Task UpdateAsync(MembershipRequest req)
        {
            _db.MembershipRequests.Update(req);
            await _db.SaveChangesAsync();
        }
    }
}
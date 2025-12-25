using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    /// <summary>
    /// Repository xử lý yêu cầu trở thành Club Leader (ClubLeaderRequest).
    /// 
    /// Công dụng: Truy vấn và lưu request từ student muốn thành leader.
    /// 
    /// Luồng dữ liệu:
    /// - CreateAsync → Add vào bảng ClubLeaderRequest (Status="pending").
    /// - GetPendingAsync → Query Status="pending".
    /// - GetApprovedAsync/GetRejectedAsync → Query theo status và sort descending ProcessedAt.
    /// - GetByIdAsync → Lấy theo Id để approve/reject.
    /// - SaveAsync → Commit thay đổi (update status sau approve/reject).
    /// 
    /// Tương tác giữa các API/service:
    /// - Student gửi request (API /leader/request): CreateAsync + SaveAsync.
    /// - Admin list pending (API /admin/leader-requests/pending): GetPendingAsync.
    /// - Admin approve/reject: GetByIdAsync → Update fields (Status, ProcessedBy, RejectReason...) → SaveAsync.
    /// - Sau approve: Add role "clubleader" ở service khác.
    /// </summary>
    public class ClubLeaderRequestRepository : IClubLeaderRequestRepository
    {
        private readonly StudentClubManagementContext _context;

        public ClubLeaderRequestRepository(StudentClubManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Thêm request mới (chưa save).
        /// 
        /// Luồng: Add vào DbSet ClubLeaderRequests.
        /// </summary>
        public async Task CreateAsync(ClubLeaderRequest request)
        {
            await _context.ClubLeaderRequests.AddAsync(request);
        }

        /// <summary>
        /// Lấy list request pending.
        /// 
        /// Luồng: Query bảng ClubLeaderRequests → Lọc Status="pending".
        /// </summary>
        public async Task<List<ClubLeaderRequest>> GetPendingAsync()
        {
            return await _context.ClubLeaderRequests
                .Where(x => x.Status != null && x.Status.ToLower() == "pending")
                .ToListAsync();
        }

        /// <summary>
        /// Lấy list request đã approve.
        /// 
        /// Luồng: Query → Lọc Status="approved" (ignore case) → Sort descending ProcessedAt.
        /// </summary>
        public async Task<List<ClubLeaderRequest>> GetApprovedAsync()
        {
            return await _context.ClubLeaderRequests
                .Where(x => x.Status.ToLower() == "approved")
                .OrderByDescending(x => x.ProcessedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy list request đã reject.
        /// 
        /// Luồng: Query → Lọc Status="rejected" (ignore case) → Sort descending ProcessedAt.
        /// </summary>
        public async Task<List<ClubLeaderRequest>> GetRejectedAsync()
        {
            return await _context.ClubLeaderRequests
                .Where(x => x.Status.ToLower() == "rejected")
                .OrderByDescending(x => x.ProcessedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy request theo Id.
        /// 
        /// Luồng: Query bảng ClubLeaderRequests → FirstOrDefault.
        /// </summary>
        public async Task<ClubLeaderRequest?> GetByIdAsync(int id)
        {
            return await _context.ClubLeaderRequests
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }

}

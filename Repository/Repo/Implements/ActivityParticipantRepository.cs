using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    /// <summary>
    /// Repository xử lý tham gia hoạt động (ActivityParticipant): CRUD và truy vấn theo membership/activity.
    /// 
    /// Công dụng: Quản lý đăng ký/hủy tham gia activity của member.
    /// 
    /// Luồng dữ liệu:
    /// - IsRegisteredAsync → Check tồn tại participant theo ActivityId/MembershipId.
    /// - GetByIdAsync → Lấy theo Id, include Activity/Membership/Club.
    /// - GetByMembershipIdAsync → Lấy list theo MembershipId, include Activity/Club, sort descending RegisterTime.
    /// - GetByActivityIdAsync → Lấy list theo ActivityId, include Membership/Account.
    /// - AddParticipantAsync → AddAsync vào DbSet.
    /// - UpdateParticipantAsync → Update entity (không await, dùng với SaveAsync).
    /// - SaveAsync → Commit thay đổi.
    /// - DeleteByActivityIdAsync → Xóa range participant theo ActivityId (khi delete activity).
    /// 
    /// Tương tác giữa các API/service:
    /// - Register activity (API /activities/register): IsRegisteredAsync (check) → AddParticipantAsync + SaveAsync (Attended=true).
    /// - Cancel (API /cancel): GetByIdAsync → Update Attended=false → UpdateParticipantAsync + SaveAsync.
    /// - Leader xem participants (API /participants): GetByActivityIdAsync.
    /// - Student xem history: GetByMembershipIdAsync.
    /// </summary>
    public class ActivityParticipantRepository : IActivityParticipantRepository
    {
        private readonly StudentClubManagementContext _context;

        public ActivityParticipantRepository(StudentClubManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Kiểm tra đã đăng ký activity chưa.
        /// 
        /// Luồng: AnyAsync trên ActivityParticipants → Lọc ActivityId/MembershipId.
        /// </summary>
        public async Task<bool> IsRegisteredAsync(int activityId, int membershipId)
        {
            return await _context.ActivityParticipants
                .AnyAsync(ap => ap.ActivityId == activityId && ap.MembershipId == membershipId);
        }

        /// <summary>
        /// Lấy participant theo Id.
        /// 
        /// Luồng: Query → Include Activity/Membership/Club → FirstOrDefault.
        /// </summary>
        public async Task<ActivityParticipant?> GetByIdAsync(int id)
        {
            return await _context.ActivityParticipants
                .Include(ap => ap.Activity!)
                .Include(ap => ap.Membership!)
                .ThenInclude(m => m.Club!)
                .FirstOrDefaultAsync(ap => ap.Id == id);
        }

        /// <summary>
        /// Lấy list participant theo MembershipId (lịch sử của member).
        /// 
        /// Luồng: Query → Include Activity/Club → Lọc MembershipId → Sort descending RegisterTime.
        /// </summary>
        public async Task<List<ActivityParticipant>> GetByMembershipIdAsync(int membershipId)
        {
            return await _context.ActivityParticipants
                .Include(ap => ap.Activity!)
                .ThenInclude(a => a.Club!)
                .Where(ap => ap.MembershipId == membershipId)
                .OrderByDescending(ap => ap.RegisterTime)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy list participant theo ActivityId (danh sách tham gia của activity).
        /// 
        /// Luồng: Query → Include Membership/Account → Lọc ActivityId.
        /// </summary>
        public async Task<List<ActivityParticipant>> GetByActivityIdAsync(int activityId)
        {
            return await _context.ActivityParticipants
                .Include(ap => ap.Membership!)
                .ThenInclude(m => m.Account!)
                .Where(ap => ap.ActivityId == activityId)
                .ToListAsync();
        }

        /// <summary>
        /// Thêm participant mới (chưa save).
        /// 
        /// Luồng: AddAsync vào DbSet ActivityParticipants.
        /// </summary>
        public async Task AddParticipantAsync(ActivityParticipant participant)
        {
            await _context.ActivityParticipants.AddAsync(participant);
        }

        /// <summary>
        /// Cập nhật participant (chưa save).
        /// 
        /// Luồng: Update entity → Task.CompletedTask (save ở SaveAsync).
        /// </summary>
        public Task UpdateParticipantAsync(ActivityParticipant participant)
        {
            _context.ActivityParticipants.Update(participant);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Lưu thay đổi (commit DB).
        /// 
        /// Luồng: SaveChangesAsync sau Add/Update.
        /// </summary>
        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Xóa tất cả participant của activity (khi delete activity).
        /// 
        /// Luồng: Query → Lọc ActivityId → RemoveRange → SaveChangesAsync.
        /// </summary>
        public async Task DeleteByActivityIdAsync(int activityId)
        {
            var list = await _context.ActivityParticipants
                .Where(x => x.ActivityId == activityId)
                .ToListAsync();

            _context.ActivityParticipants.RemoveRange(list);
            await _context.SaveChangesAsync();
        }

        public async Task<List<ActivityParticipant>> GetByAccountIdAsync(int? accountId)
        {
            if (!accountId.HasValue)
                return new List<ActivityParticipant>();

            return await _context.ActivityParticipants
                .Include(p => p.Activity!)
                    .ThenInclude(a => a.Club!)
                .Where(p => p.AccountId == accountId.Value)
                .ToListAsync();
        }
    }
}
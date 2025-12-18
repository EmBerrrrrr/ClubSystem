using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    /// <summary>
    /// Repository xử lý hoạt động (Activity): CRUD và kiểm tra quyền leader.
    /// 
    /// Công dụng: Truy vấn và quản lý activity của CLB.
    /// 
    /// Luồng dữ liệu:
    /// - GetAllAsync → List tất cả Activity, sort descending StartTime.
    /// - GetByClubAsync → Lấy theo ClubId, sort descending StartTime.
    /// - GetByIdAsync → FirstOrDefault theo Id.
    /// - AddAsync/UpdateAsync/DeleteAsync → Thao tác trên DbSet Activities → SaveChangesAsync.
    /// - IsLeaderOfClubAsync → Check quyền leader từ ClubLeaders (IsActive=true).
    /// 
    /// Tương tác giữa các API/service:
    /// - Create activity (API /activities/create): AddAsync + SaveChangesAsync (sau check leader).
    /// - List activities (API /activities): GetAllAsync.
    /// - Detail (API /activities/{id}): GetByIdAsync.
    /// - Check quyền trước create/update: IsLeaderOfClubAsync.
    /// </summary>
    public class ActivityRepository : IActivityRepository
    {
        private readonly StudentClubManagementContext _context;

        public ActivityRepository(StudentClubManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy tất cả Activity.
        /// 
        /// Luồng: Query Activities → Sort descending StartTime.
        /// </summary>
        public Task<List<Activity>> GetAllAsync()
            => _context.Activities.OrderByDescending(x => x.StartTime).ToListAsync();

        /// <summary>
        /// Lấy Activity theo ClubId.
        /// 
        /// Luồng: Query Activities → Lọc ClubId → Sort descending StartTime.
        /// </summary>
        public Task<List<Activity>> GetByClubAsync(int clubId)
            => _context.Activities
                .Where(x => x.ClubId == clubId)
                .OrderByDescending(x => x.StartTime)
                .ToListAsync();

        /// <summary>
        /// Lấy Activity theo Id.
        /// 
        /// Luồng: FirstOrDefault theo Id.
        /// </summary>
        public Task<Activity?> GetByIdAsync(int id)
            => _context.Activities.FirstOrDefaultAsync(x => x.Id == id);

        /// <summary>
        /// Thêm Activity mới (save luôn).
        /// 
        /// Luồng: Add → SaveChangesAsync.
        /// </summary>
        public async Task AddAsync(Activity activity)
        {
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Cập nhật Activity (save luôn).
        /// 
        /// Luồng: Update → SaveChangesAsync.
        /// </summary>
        public async Task UpdateAsync(Activity activity)
        {
            _context.Activities.Update(activity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Xóa Activity (save luôn).
        /// 
        /// Luồng: Remove → SaveChangesAsync.
        /// </summary>
        public async Task DeleteAsync(Activity activity)
        {
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Kiểm tra account có phải leader active của club.
        /// 
        /// Luồng: AnyAsync trên ClubLeaders → Lọc ClubId/AccountId/IsActive.
        /// </summary>
        public Task<bool> IsLeaderOfClubAsync(int clubId, int accountId)
            => _context.ClubLeaders.AnyAsync(x =>
                x.ClubId == clubId &&
                x.AccountId == accountId &&
                x.IsActive == true);
    }
}
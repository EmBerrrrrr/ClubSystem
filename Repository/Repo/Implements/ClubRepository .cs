using Microsoft.EntityFrameworkCore;
using DTO;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    public class ClubRepository : IClubRepository
    {
        /// <summary>
        /// Repository xử lý các thao tác liên quan đến Club (câu lạc bộ).
        /// 
        /// Công dụng: Truy vấn, thêm, cập nhật, xóa CLB, kiểm tra quyền leader.
        /// 
        /// Luồng dữ liệu:
        /// - AddAsync/UpdateAsync/DeleteAsync → Thao tác trên DbSet Clubs → SaveChangesAsync.
        /// - GetByIdAsync → Find theo Id.
        /// - GetAllAsync → List toàn bộ.
        /// - GetByLeaderIdAsync → Join ClubLeaders (IsActive=true) → Lấy Club của leader.
        /// - IsLeaderOfClubAsync → Check tồn tại ClubLeader active.
        /// - GetLeaderAccountIdsByClubIdAsync → Lấy list AccountId leader active của club.
        /// - GetDetailWithActivitiesAsync → Include Activities để lấy chi tiết club + hoạt động.
        /// 
        /// Tương tác giữa các API/service:
        /// - Create club (API /club/create): AddAsync + SaveChangesAsync + Add ClubLeader.
        /// - List clubs (API /clubs): GetAllAsync.
        /// - Check quyền leader cho activity/membership: IsLeaderOfClubAsync.
        /// - Lock/unlock club: GetByIdAsync → Update Status → SaveChangesAsync.
        /// - Noti khi lock: GetLeaderAccountIdsByClubIdAsync → Push noti cho leaders.
        /// </summary>
        private readonly StudentClubManagementContext _context;

        public ClubRepository(StudentClubManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Thêm Club mới (chưa save).
        /// 
        /// Luồng: Add vào DbSet Clubs.
        /// </summary>
        public async Task AddAsync(Club club)
        {
            _context.Clubs.Add(club);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Cập nhật Club (chưa save).
        /// 
        /// Luồng: Update entity trong DbSet Clubs.
        /// </summary>
        public async Task UpdateAsync(Club club)
        {
            _context.Clubs.Update(club);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy chi tiết Club kèm Activities.
        /// 
        /// Luồng: Query Clubs → Include Activities → FirstOrDefault theo Id.
        /// </summary>
        public async Task<Club?> GetDetailWithActivitiesAsync(int id)
        {
            return await _context.Clubs
                .Include(x => x.Activities)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <summary>
        /// Xóa Club (chưa save).
        /// 
        /// Luồng: Remove entity từ DbSet Clubs.
        /// </summary>
        public async Task DeleteAsync(Club club)
        {
            _context.Clubs.Remove(club);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Lấy Club theo Id.
        /// 
        /// Luồng: FindAsync trên DbSet Clubs.
        /// </summary>
        public async Task<Club?> GetByIdAsync(int id)
        {
            return await _context.Clubs.FindAsync(id);
        }

        /// <summary>
        /// Lấy tất cả Club.
        /// 
        /// Luồng: ToListAsync trên DbSet Clubs.
        /// </summary>
        public async Task<List<Club>> GetAllAsync()
        {
            return await _context.Clubs.ToListAsync();
        }

        /// <summary>
        /// Lấy list Club mà leader quản lý (active).
        /// 
        /// Luồng: Query ClubLeaders → Include Club → Lọc AccountId và IsActive.
        /// </summary>
        public async Task<List<Club>> GetByLeaderIdAsync(int leaderId)
        {
            return await _context.ClubLeaders
               .Where(x => x.AccountId == leaderId && x.IsActive == true)
               .Include(x => x.Club)
               .Select(x => x.Club)
               .ToListAsync();
        }

        /// <summary>
        /// Kiểm tra account có phải leader active của club.
        /// 
        /// Luồng: AnyAsync trên ClubLeaders → Lọc ClubId/AccountId/IsActive.
        /// </summary>
        public async Task<bool> IsLeaderOfClubAsync(int clubId, int leaderId)
        {
            return await _context.ClubLeaders
                .AnyAsync(x =>
                    x.ClubId == clubId &&
                    x.AccountId == leaderId &&
                    x.IsActive == true);
        }

        /// <summary>
        /// Lấy list AccountId leader active của club.
        /// 
        /// Luồng: Query ClubLeaders → Lọc ClubId/IsActive → Select AccountId.
        /// </summary>
        public async Task<List<int>> GetLeaderAccountIdsByClubIdAsync(int clubId)
        {
            return await _context.ClubLeaders
                .Where(x => x.ClubId == clubId && x.IsActive == true)
                .Select(x => x.AccountId)
                .ToListAsync();
        }
    }

}


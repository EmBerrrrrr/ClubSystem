using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    /// <summary>
    /// Repository xử lý thành viên CLB (Membership): Kiểm tra, lấy list, thêm/update.
    /// 
    /// Công dụng: Quản lý membership active/pending, đếm thành viên.
    /// 
    /// Luồng dữ liệu:
    /// - IsMemberAsync → Check tồn tại membership active theo AccountId/ClubId.
    /// - GetMembershipsAsync → Lấy list active của accountId, include Club.
    /// - GetAllMembershipsAsync → Lấy all (mọi status) của accountId, include Club.
    /// - GetMembershipsByClubIdAsync → Lấy list theo ClubId, include Account/Club.
    /// - GetActiveMemberCountsByClubIdsAsync → Group by ClubId, count Status="active".
    /// - GetActiveMemberCountByClubIdAsync → Count Status="active" theo ClubId.
    /// - GetMembershipByAccountAndClubAsync → FirstOrDefault theo AccountId/ClubId.
    /// - GetMembershipByIdAsync → Lấy theo Id, include Account/Club.
    /// - AddMembershipAsync → AddAsync.
    /// - UpdateMembership → Update entity.
    /// - SaveAsync → Commit thay đổi.
    /// 
    /// Tương tác giữa các API/service:
    /// - Approve request (API /membership/approve): IsMemberAsync (check) → AddMembershipAsync + SaveAsync (Status="active"/"pending_payment").
    /// - Student xem my clubs: GetMembershipsAsync.
    /// - Leader xem members: GetMembershipsByClubIdAsync.
    /// - Admin monitoring: GetActiveMemberCountByClubIdAsync.
    /// - Update status khi payment success: GetMembershipByIdAsync → Update Status="active" → UpdateMembership + SaveAsync.
    /// </summary>
    public class MembershipRepository : IMembershipRepository
    {
        private readonly StudentClubManagementContext _db;

        public MembershipRepository(StudentClubManagementContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Kiểm tra account có phải member active của club.
        /// 
        /// Luồng: AnyAsync → Lọc AccountId/ClubId/Status="active" (ignore case).
        /// </summary>
        public async Task<bool> IsMemberAsync(int accountId, int clubId)
        {
            return await _db.Memberships
                .AnyAsync(x =>
                    x.AccountId == accountId &&
                    x.ClubId == clubId &&
                    x.Status.ToLower() == "active");
        }

        /// <summary>
        /// Lấy list membership active của account.
        /// 
        /// Luồng: Query → Lọc AccountId/Status="active" → Include Club.
        /// </summary>
        public async Task<List<Membership>> GetMembershipsAsync(int accountId)
        {
            return await _db.Memberships
                .Where(x => x.AccountId == accountId && x.Status != null && x.Status.ToLower() == "active")
                .Include(x => x.Club)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy list tất cả membership của account (mọi status).
        /// 
        /// Luồng: Query → Lọc AccountId → Include Club.
        /// </summary>
        public async Task<List<Membership>> GetAllMembershipsAsync(int accountId)
        {
            return await _db.Memberships
                .Where(x => x.AccountId == accountId)
                .Include(x => x.Club)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy list membership theo ClubId.
        /// 
        /// Luồng: Query → Lọc ClubId → Include Account/Club.
        /// </summary>
        public async Task<List<Membership>> GetMembershipsByClubIdAsync(int clubId)
        {
            return await _db.Memberships
                .Where(x => x.ClubId == clubId)
                .Include(x => x.Account)
                .Include(x => x.Club)
                .ToListAsync();
        }

        /// <summary>
        /// Đếm thành viên active theo list ClubIds.
        /// 
        /// Luồng: Query Memberships → Lọc ClubIds/Status="Active" → GroupBy ClubId → ToDictionary count.
        /// </summary>
        public async Task<Dictionary<int, int>> GetActiveMemberCountsByClubIdsAsync(List<int> clubIds)
        {
            if (!clubIds.Any())
                return new Dictionary<int, int>();

            return await _db.Memberships  // Dùng đúng DbSet: Memberships (entity Membership)
                .Where(m => clubIds.Contains(m.ClubId) && m.Status != null && m.Status.ToLower() == "active")
                .GroupBy(m => m.ClubId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Đếm thành viên active theo ClubId.
        /// 
        /// Luồng: CountAsync → Lọc ClubId/Status="active" (ignore case).
        /// </summary>
        public async Task<int> GetActiveMemberCountByClubIdAsync(int clubId)
        {
            return await _db.Memberships
                .Where(x => x.ClubId == clubId && x.Status != null && x.Status.ToLower() == "active")
                .CountAsync();
        }

        /// <summary>
        /// Lấy membership theo AccountId/ClubId.
        /// 
        /// Luồng: FirstOrDefaultAsync → Lọc AccountId/ClubId.
        /// </summary>
        public async Task<Membership?> GetMembershipByAccountAndClubAsync(int accountId, int clubId)
        {
            return await _db.Memberships
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.ClubId == clubId);
        }

        /// <summary>
        /// Lấy membership theo Id.
        /// 
        /// Luồng: FirstOrDefaultAsync → Include Account/Club.
        /// </summary>
        public async Task<Membership?> GetMembershipByIdAsync(int membershipId)
        {
            return await _db.Memberships
                .Include(x => x.Account)
                .Include(x => x.Club)
                .FirstOrDefaultAsync(x => x.Id == membershipId);
        }

        /// <summary>
        /// Thêm membership mới (chưa save).
        /// 
        /// Luồng: AddAsync vào DbSet Memberships.
        /// </summary>
        public async Task AddMembershipAsync(Membership member)
        {
            await _db.Memberships.AddAsync(member);
        }

        /// <summary>
        /// Cập nhật membership (không save).
        /// 
        /// Luồng: Update entity trong DbSet Memberships.
        /// </summary>
        public void UpdateMembership(Membership membership)
        {
            _db.Memberships.Update(membership);
        }

        public Task DeleteMembership(Membership membership)
        {
            _db.Memberships.Remove(membership);
            return Task.CompletedTask;
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
    }
}
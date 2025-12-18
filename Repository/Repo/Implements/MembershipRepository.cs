using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    public class MembershipRepository : IMembershipRepository
    {
        private readonly StudentClubManagementContext _db;

        public MembershipRepository(StudentClubManagementContext db)
        {
            _db = db;
        }

        public async Task<bool> IsMemberAsync(int accountId, int clubId)
        {
            return await _db.Memberships
                .AnyAsync(x =>
                    x.AccountId == accountId &&
                    x.ClubId == clubId &&
                    x.Status.ToLower() == "active");
        }

        public async Task<List<Membership>> GetMembershipsAsync(int accountId)
        {
            return await _db.Memberships
                .Where(x => x.AccountId == accountId && x.Status == "active")
                .Include(x => x.Club)
                .ToListAsync();
        }

        public async Task<List<Membership>> GetAllMembershipsAsync(int accountId)
        {
            return await _db.Memberships
                .Where(x => x.AccountId == accountId)
                .Include(x => x.Club)
                .ToListAsync();
        }

        public async Task<List<Membership>> GetMembershipsByClubIdAsync(int clubId)
        {
            return await _db.Memberships
                .Where(x => x.ClubId == clubId)
                .Include(x => x.Account)
                .Include(x => x.Club)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetActiveMemberCountsByClubIdsAsync(List<int> clubIds)
        {
            if (!clubIds.Any())
                return new Dictionary<int, int>();

            return await _db.Memberships  // Dùng đúng DbSet: Memberships (entity Membership)
                .Where(m => clubIds.Contains(m.ClubId) && m.Status == "Active")
                .GroupBy(m => m.ClubId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        public async Task<int> GetActiveMemberCountByClubIdAsync(int clubId)
        {
            return await _db.Memberships
                .Where(x => x.ClubId == clubId && x.Status != null && x.Status.ToLower() == "active")
                .CountAsync();
        }

        public async Task<Membership?> GetMembershipByAccountAndClubAsync(int accountId, int clubId)
        {
            return await _db.Memberships
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.ClubId == clubId);
        }

        public async Task<Membership?> GetMembershipByIdAsync(int membershipId)
        {
            return await _db.Memberships
                .Include(x => x.Account)
                .Include(x => x.Club)
                .FirstOrDefaultAsync(x => x.Id == membershipId);
        }

        public async Task AddMembershipAsync(Membership member)
        {
            await _db.Memberships.AddAsync(member);
        }

        public void UpdateMembership(Membership membership)
        {
            _db.Memberships.Update(membership);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}

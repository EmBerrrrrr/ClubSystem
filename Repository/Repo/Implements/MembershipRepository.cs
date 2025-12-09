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

        public async Task<Membership?> GetMembershipByAccountAndClubAsync(int accountId, int clubId)
        {
            return await _db.Memberships
                .FirstOrDefaultAsync(x => x.AccountId == accountId && x.ClubId == clubId);
        }

        public async Task AddMembershipAsync(Membership member)
        {
            await _db.Memberships.AddAsync(member);
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}

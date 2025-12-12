using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    public class MembershipRequestRepository : IMembershipRequestRepository
    {
        private readonly StudentClubManagementContext _db;


        public MembershipRequestRepository(StudentClubManagementContext db)
        {
            _db = db;
        }

        public async Task<bool> HasPendingRequestAsync(int accountId, int clubId)
        {
            return await _db.MembershipRequests
                .AnyAsync(x =>
                    x.AccountId == accountId &&
                    x.ClubId == clubId &&
                    x.Status == "Pending");
        }

        public async Task CreateRequestAsync(MembershipRequest request)
        {
            await _db.MembershipRequests.AddAsync(request);
        }

        public async Task<List<MembershipRequest>> GetRequestsOfAccountAsync(int accountId)
        {
            return await _db.MembershipRequests
                .Where(x => x.AccountId == accountId)
                .Include(x => x.Club)
                .Include(x => x.Account)
                .OrderByDescending(x => x.RequestDate)
                .ToListAsync();
        }

        public async Task SaveAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task<MembershipRequest?> GetByIdAsync(int id)
        {
            return await _db.MembershipRequests.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<MembershipRequest>> GetPendingRequestsByClubAsync(int clubId)
        {
            return await _db.MembershipRequests
                .Where(r => r.ClubId == clubId && r.Status == "Pending")
                .Include(r => r.Account)
                .ToListAsync();
        }

        public async Task<List<MembershipRequest>> GetAllRequestsByClubAsync(int clubId)
        {
            return await _db.MembershipRequests
                .Where(r => r.ClubId == clubId)
                .Include(r => r.Account)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        public async Task UpdateAsync(MembershipRequest req)
        {
            _db.MembershipRequests.Update(req);
            await _db.SaveChangesAsync();
        }
    }
}

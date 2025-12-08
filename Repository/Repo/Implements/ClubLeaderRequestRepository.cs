using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    public class ClubLeaderRequestRepository : IClubLeaderRequestRepository
    {
        private readonly StudentClubManagementContext _context;

        public ClubLeaderRequestRepository(StudentClubManagementContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(ClubLeaderRequest request)
        {
            await _context.ClubLeaderRequests.AddAsync(request);
        }

        public async Task<List<ClubLeaderRequest>> GetPendingAsync()
        {
            return await _context.ClubLeaderRequests
                .Where(x => x.Status == "PENDING")
                .ToListAsync();
        }

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

using Microsoft.EntityFrameworkCore;
using DTO;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements
{
    public class ClubRepository : IClubRepository
    {
        private readonly StudentClubManagementContext _context;

        public ClubRepository(StudentClubManagementContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Club club)
        {
            _context.Clubs.Add(club);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Club club)
        {
            _context.Clubs.Update(club);
            await _context.SaveChangesAsync();
        }
        public async Task<Club?> GetDetailWithActivitiesAsync(int id)
        {
            return await _context.Clubs
                .Include(x => x.Activities)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task DeleteAsync(Club club)
        {
            _context.Clubs.Remove(club);
            await _context.SaveChangesAsync();
        }

        public async Task<Club?> GetByIdAsync(int id)
        {
            return await _context.Clubs.FindAsync(id);
        }

        public async Task<List<Club>> GetAllAsync()
        {
            return await _context.Clubs.ToListAsync();
        }

        public async Task<List<Club>> GetByLeaderIdAsync(int leaderId)
        {
            return await _context.ClubLeaders
                .Where(x => x.AccountId == leaderId && x.IsActive == true)
                .Include(x => x.Club)
                .Select(x => x.Club)
                .ToListAsync();
        }

        public async Task<bool> IsLeaderOfClubAsync(int clubId, int accountId)
        {
            return await _context.ClubLeaders.AnyAsync(x =>
                x.ClubId == clubId &&
                x.AccountId == accountId &&
                x.IsActive == true);
        }
    }

}


using Microsoft.EntityFrameworkCore;
using DTO;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Repository.Repo.Implements;

public class ClubRepository : IClubRepository
{
    private readonly StudentClubManagementContext _context;

    public ClubRepository(StudentClubManagementContext context)
    {
        _context = context;
    }

    public async Task<List<ClubDTO>> GetAllClubsAsync()
        => await _context.Clubs
            .Select(c => new ClubDTO
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageClubsUrl = c.ImageClubsUrl,
                MembershipFee = c.MembershipFee,
                Status = c.Status
            })
            .ToListAsync();

    public async Task<Club?> GetClubDetailAsync(int id)
        => await _context.Clubs
            .Include(c => c.ClubLeaders)
            .Include(c => c.Activities)
            .Include(c => c.Memberships)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> SendJoinRequestAsync(int accountId, int clubId)
    {
        var exists = await _context.MembershipRequests
            .AnyAsync(r => r.AccountId == accountId && r.ClubId == clubId && r.Status == "pending");

        if (exists) return false;

        var request = new MembershipRequest
        {
            AccountId = accountId,
            ClubId = clubId,
            RequestDate = DateTime.Now,
            Status = "pending"
        };

        _context.MembershipRequests.Add(request);
        await _context.SaveChangesAsync();
        return true;
    }
}

using DTO;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Services;

public class ClubService : IClubService
{
    private readonly IClubRepository _repo;

    public ClubService(IClubRepository repo)
    {
        _repo = repo;
    }

    public Task<List<ClubDTO>> GetAllClubsAsync()
        => _repo.GetAllClubsAsync();

    public async Task<ClubDTO?> GetClubDetailAsync(int id)
    {
        var club = await _repo.GetClubDetailAsync(id);
        if (club == null) return null;
        return new ClubDTO
        {
            Id = club.Id,
            Name = club.Name,
            // Map other properties as needed
        };
    }

    public Task<bool> SendJoinRequestAsync(int accountId, int clubId)
        => _repo.SendJoinRequestAsync(accountId, clubId);
}

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

    public Task<Club?> GetClubDetailAsync(int id)
        => _repo.GetClubDetailAsync(id);

    public Task<bool> SendJoinRequestAsync(int accountId, int clubId)
        => _repo.SendJoinRequestAsync(accountId, clubId);
}

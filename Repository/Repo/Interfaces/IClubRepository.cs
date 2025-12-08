using DTO;
using Repository.Models;

namespace Repository.Repo.Interfaces;

public interface IClubRepository
{
    Task<List<ClubDTO>> GetAllClubsAsync();
    Task<Club?> GetClubDetailAsync(int id);
    Task<bool> SendJoinRequestAsync(int accountId, int clubId);
}

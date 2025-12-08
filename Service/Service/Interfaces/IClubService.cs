using DTO;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Service.Services;

public interface IClubService
{
    Task<List<ClubDTO>> GetAllClubsAsync();
    Task<Club?> GetClubDetailAsync(int id);
    Task<bool> SendJoinRequestAsync(int accountId, int clubId);
}


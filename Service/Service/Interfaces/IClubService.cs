using DTO;
using Repository.Models;

namespace Service.Services;

public interface IClubService
{
    Task<List<ClubDTO>> GetAllClubsAsync();
    Task<ClubDTO?> GetClubDetailAsync(int id);
    Task<bool> SendJoinRequestAsync(int accountId, int clubId);
}


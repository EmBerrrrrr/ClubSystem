using DTO;
using DTO.DTO.Club;
using DTO.DTO.Clubs;
using Repository.Models;

namespace Service.Services.Interfaces
{
    public interface IClubService
    {
        Task<List<ClubDto>> GetMyClubsAsync(int accountId);
        Task<List<ClubDto>> GetAllClubsForAdminAsync();
        Task<ClubDetailDto?> GetDetailAsync(int id);
        Task<ClubDto> CreateAsync(CreateClubDto dto, int leaderId);
        Task UpdateAsync(int clubId, UpdateClubDto dto, int accountId, bool isAdmin);
        Task DeleteAsync(int clubId, int accountId, bool isAdmin);
        Task<List<int>> GetLeaderAccountIdsByClubIdAsync(int clubId);
        Task<Dictionary<int, string?>> GetActiveLeaderNamesByClubIdsAsync(List<int> clubIds);


    }

}


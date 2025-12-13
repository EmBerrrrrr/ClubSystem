using DTO.DTO.Club;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IAdminClubService
    {
        Task<List<ClubMonitoringDto>> GetAllClubsForMonitoringAsync();
        Task<ClubDetailMonitoringDto?> GetClubDetailForMonitoringAsync(int clubId);
    }
}


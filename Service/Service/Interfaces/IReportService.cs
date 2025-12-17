using DTO.DTO.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Interfaces
{
    public interface IReportService
    {
        Task<ClubReportResponse> GetClubReportAsync(int clubId, int leaderId);
        Task<List<ClubReportResponse>> GetMyClubsReportAsync(int leaderAccountId);
        Task<bool> IsLeaderOfClubAsync(int leaderAccountId, int clubId);

    }
}

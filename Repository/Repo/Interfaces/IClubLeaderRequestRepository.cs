using Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repo.Interfaces
{
    public interface IClubLeaderRequestRepository
    {
        Task CreateAsync(ClubLeaderRequest request);
        Task<List<ClubLeaderRequest>> GetPendingAsync();
        Task<ClubLeaderRequest?> GetByIdAsync(int id);
        Task SaveAsync();
    }

}

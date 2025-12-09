using DTO;
using DTO.DTO.Activity;
using DTO.DTO.Club;
using DTO.DTO.Clubs;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Services.Interfaces;

namespace Service.Service.Implements
{
    public class ClubService : IClubService
    {
        private readonly IClubRepository _repo;
        private readonly StudentClubManagementContext _context;

        public ClubService(IClubRepository repo, StudentClubManagementContext context)
        {
            _repo = repo;
            _context = context;
        }

        // FOR CLUB LEADER
        public async Task<List<ClubDto>> GetMyClubsAsync(int accountId)
        {
            var clubs = await _repo.GetByLeaderIdAsync(accountId);

            return clubs.Select(c => MapToDto(c)).ToList();
        }

        // FOR ADMIN
        public async Task<List<ClubDto>> GetAllClubsForAdminAsync()
        {
            var clubs = await _repo.GetAllAsync();

            return clubs.Select(c => MapToDto(c)).ToList();
        }

        public async Task<ClubDto> CreateAsync(CreateClubDto dto, int leaderId)
        {
            var club = new Club
            {
                Name = dto.Name,
                Description = dto.Description,
                EstablishedDate = dto.EstablishedDate.HasValue
                    ? DateOnly.FromDateTime(dto.EstablishedDate.Value)
                    : null,
                ImageClubsUrl = dto.ImageClubsUrl,
                MembershipFee = dto.MembershipFee,
                Status = "Active"
            };

            await _repo.AddAsync(club);

            // Tạo record leader
            _context.ClubLeaders.Add(new ClubLeader
            {
                ClubId = club.Id,
                AccountId = leaderId,
                IsActive = true,
                StartDate = DateOnly.FromDateTime(DateTime.Now)
            });

            await _context.SaveChangesAsync();

            return MapToDto(club);
        }

        public async Task UpdateAsync(int clubId, UpdateClubDto dto, int accountId, bool isAdmin)
        {
            if (!isAdmin)
            {
                var isLeader = await _repo.IsLeaderOfClubAsync(clubId, accountId);
                if (!isLeader)
                    throw new UnauthorizedAccessException("Bạn không phải leader của club này.");
            }

            var club = await _repo.GetByIdAsync(clubId)
                ?? throw new Exception("Không tìm thấy club");

            club.Name = dto.Name;
            club.Description = dto.Description;
            club.EstablishedDate = dto.EstablishedDate.HasValue
                ? DateOnly.FromDateTime(dto.EstablishedDate.Value)
                : null;
            club.ImageClubsUrl = dto.ImageClubsUrl;
            club.MembershipFee = dto.MembershipFee;
            club.Status = dto.Status;

            await _repo.UpdateAsync(club);
        }

        public async Task DeleteAsync(int clubId, int accountId, bool isAdmin)
        {
            if (!isAdmin)
            {
                var isLeader = await _repo.IsLeaderOfClubAsync(clubId, accountId);
                if (!isLeader)
                    throw new UnauthorizedAccessException("Không phải leader club.");
            }

            var club = await _repo.GetByIdAsync(clubId)
                ?? throw new Exception("Club không tồn tại");

            await _repo.DeleteAsync(club);
        }
        public async Task<ClubDetailDto?> GetDetailAsync(int id)
        {
            var club = await _repo.GetDetailWithActivitiesAsync(id);

            if (club == null) return null;

            return new ClubDetailDto
            {
                Id = club.Id,
                Name = club.Name,
                Description = club.Description,
                ImageUrl = club.ImageClubsUrl,
                Status = club.Status,

                Activities = club.Activities
                    .OrderByDescending(x => x.StartTime)
                    .Select(a => new ActivityDto
                    {
                        Id = a.Id,
                        ClubId = a.ClubId,
                        Title = a.Title,
                        Description = a.Description,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        Location = a.Location,
                        Status = a.Status,
                        CreatedBy = a.CreatedBy
                    })
                    .ToList()
            };
        }

        private ClubDto MapToDto(Club c)
        {
            return new ClubDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageClubsUrl = c.ImageClubsUrl,
                EstablishedDate = c.EstablishedDate?.ToDateTime(TimeOnly.MinValue),
                MembershipFee = c.MembershipFee,
                Status = c.Status
            };
        }
    }

}

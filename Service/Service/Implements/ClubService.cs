using DTO;
using DTO.DTO.Activity;
using DTO.DTO.Club;
using DTO.DTO.Clubs;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Repository.Repo.Interfaces;
using Service.Helper;
using Service.Service.Interfaces;
using Service.Services.Interfaces;
using System;
using System.Linq;

namespace Service.Service.Implements
{
    public class ClubService : IClubService
    {
        private readonly IClubRepository _repo;
        private readonly StudentClubManagementContext _context;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IMembershipRepository _membershipRepo;
        private readonly INotificationService _noti;

        public ClubService(
            IClubRepository repo, 
            StudentClubManagementContext context,
            IPaymentRepository paymentRepo,
            IMembershipRepository membershipRepo,
            INotificationService noti)
        {
            _repo = repo;
            _context = context;
            _paymentRepo = paymentRepo;
            _membershipRepo = membershipRepo;
            _noti = noti;
        }

        // FOR CLUB LEADER
        public async Task<List<ClubDto>> GetMyClubsAsync(int accountId)
        {
            var clubs = await _repo.GetByLeaderIdAsync(accountId);

            return clubs.Select(c => MapToDto(c)).ToList();
        }

        // FOR ADMIN - Bao gồm thông tin monitoring (memberCount, totalRevenue)
        public async Task<List<ClubDto>> GetAllClubsForAdminAsync()
        {
            var clubs = await _repo.GetAllAsync();
            if (!clubs.Any()) return new List<ClubDto>();

            var clubIds = clubs.Select(c => c.Id).ToList();

            var leaderMap = await GetActiveLeaderNamesByClubIdsAsync(clubIds);

            var result = new List<ClubDto>();

            foreach (var club in clubs)
            {
                if (club == null || string.IsNullOrEmpty(club.Name))
                    continue;

                var memberCount = await _membershipRepo
                    .GetActiveMemberCountByClubIdAsync(club.Id);

                var totalRevenue = await _paymentRepo
                    .GetTotalRevenueFromMembersByClubIdAsync(club.Id);

                var dto = MapToDto(club);
                dto.MemberCount = memberCount;
                dto.TotalRevenue = totalRevenue;

                dto.LeaderName = leaderMap.GetValueOrDefault(club.Id);

                result.Add(dto);
            }

            return result.OrderBy(c => c.Name).ToList();
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
                Status = "Active",
                Location = dto.Location,
                ContactEmail = dto.ContactEmail,
                ContactPhone = dto.ContactPhone,
                ActivityFrequency = dto.ActivityFrequency
            };

            await _repo.AddAsync(club);

            // Tạo record leader
            _context.ClubLeaders.Add(new ClubLeader
            {
                ClubId = club.Id,
                AccountId = leaderId,
                IsActive = true,
                StartDate = DateOnly.FromDateTime(DateTimeExtensions.NowVietnam())
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
            club.Location = dto.Location;
            club.ContactEmail = dto.ContactEmail;
            club.ContactPhone = dto.ContactPhone;
            club.ActivityFrequency = dto.ActivityFrequency;

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

            var club = await _context.Clubs
                .Include(c => c.Activities)
                .Include(c => c.Memberships)
                .Include(c => c.MembershipRequests)
                .Include(c => c.ClubLeaders)
                .FirstOrDefaultAsync(c => c.Id == clubId)
                ?? throw new Exception("Club không tồn tại");

            var activityIds = club.Activities.Select(a => a.Id).ToList();
            var membershipIds = club.Memberships.Select(m => m.Id).ToList();

            var activityParticipants = _context.ActivityParticipants
                .Where(x =>
                    activityIds.Contains(x.ActivityId) ||
                    membershipIds.Contains(x.MembershipId));

            _context.ActivityParticipants.RemoveRange(activityParticipants);

            var payments = _context.Payments
                .Where(p => p.ClubId == clubId);

            _context.Payments.RemoveRange(payments);

            _context.MembershipRequests.RemoveRange(club.MembershipRequests);

            _context.Memberships.RemoveRange(club.Memberships);

            _context.Activities.RemoveRange(club.Activities);

            _context.ClubLeaders.RemoveRange(club.ClubLeaders);

            _context.Clubs.Remove(club);

            await _context.SaveChangesAsync();
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
                Location = club.Location,
                ContactEmail = club.ContactEmail,
                ContactPhone = club.ContactPhone,
                ActivityFrequency = club.ActivityFrequency,

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
                        CreatedBy = a.CreatedBy,
                        ImageActsUrl = a.ImageActsUrl,
                        AvatarPublicId = a.AvatarPublicId
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
                AvatarPublicId = c.AvatarPublicId,
                EstablishedDate = c.EstablishedDate?.ToDateTime(TimeOnly.MinValue),
                MembershipFee = c.MembershipFee,
                Status = c.Status,
                Location = c.Location,
                ContactEmail = c.ContactEmail,
                ContactPhone = c.ContactPhone,
                ActivityFrequency = c.ActivityFrequency
            };
        }

        public async Task<List<int>> GetLeaderAccountIdsByClubIdAsync(int clubId)
        {
            return await _context.ClubLeaders
                .Where(x => x.ClubId == clubId && x.IsActive == true)
                .Select(x => x.AccountId)
                .ToListAsync();
        }

        public async Task<Dictionary<int, string?>> GetActiveLeaderNamesByClubIdsAsync(
            List<int> clubIds)
        {
            return await _context.ClubLeaders
                .AsNoTracking()
                .Include(cl => cl.Account)
                .Where(cl =>
                    clubIds.Contains(cl.ClubId) &&
                    cl.IsActive == true)
                .GroupBy(cl => cl.ClubId)
                .Select(g => new
                {
                    ClubId = g.Key,
                    LeaderName = g
                        .Select(x => x.Account.FullName)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.ClubId, x => x.LeaderName);
        }



    }
}

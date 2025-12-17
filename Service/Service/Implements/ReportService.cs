using DTO.DTO.Report;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class ReportService : IReportService
    {
        private readonly StudentClubManagementContext _context;

        public ReportService(StudentClubManagementContext context)
        {
            _context = context;
        }

        private async Task<ClubReportResponse> BuildClubReportAsync(int clubId)
        {
            var club = await _context.Clubs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clubId);

            if (club == null)
                throw new Exception("Club not found");

            var leader = await _context.ClubLeaders
                .AsNoTracking()
                .Where(cl => cl.ClubId == clubId && cl.IsActive == true)
                .Select(cl => new LeaderInfoDto
                {
                    LeaderName = cl.Account.FullName,
                    StartDate = cl.StartDate
                })
                .FirstOrDefaultAsync();

            var totalMembers = await _context.Memberships
                .CountAsync(m => m.ClubId == clubId);

            var activeMembers = await _context.Memberships
                .CountAsync(m => m.ClubId == clubId && m.Status == "ACTIVE");

            var activities = await _context.Activities
                .Where(a => a.ClubId == clubId)
                .Select(a => new ActivityReportDto
                {
                    ActivityId = a.Id,
                    Title = a.Title,
                    StartTime = a.StartTime,
                    Location = a.Location,
                    Participants = a.ActivityParticipants.Count,
                    Status = a.Status
                })
                .ToListAsync();

            return new ClubReportResponse
            {
                Club = new ClubInfoDto
                {
                    ClubId = club.Id,
                    ClubName = club.Name,
                    Description = club.Description,
                    ContactEmail = club.ContactEmail,
                    ContactPhone = club.ContactPhone,
                    ActivityFrequency = club.ActivityFrequency
                },
                Leader = leader,
                Statistics = new ClubStatisticsDto
                {
                    TotalMembers = totalMembers,
                    ActiveMembers = activeMembers,
                    NewMembers = 0,
                    TotalActivities = activities.Count,
                    TotalIncome = 0
                },
                Activities = activities
            };
        }

        public async Task<ClubReportResponse> GetClubReportAsync(
            int clubId,
            int leaderAccountId)
        {
            var isLeader = await _context.ClubLeaders
                .AnyAsync(cl =>
                    cl.ClubId == clubId &&
                    cl.AccountId == leaderAccountId &&
                    cl.IsActive == true);

            if (!isLeader)
                throw new UnauthorizedAccessException("Bạn không phải là trưởng câu lạc bộ này.");

            return await BuildClubReportAsync(clubId);
        }

        public async Task<List<ClubReportResponse>> GetMyClubsReportAsync(
            int leaderAccountId)
        {
            var clubIds = await _context.ClubLeaders
                .Where(cl => cl.AccountId == leaderAccountId && cl.IsActive == true)
                .Select(cl => cl.ClubId)
                .ToListAsync();

            var result = new List<ClubReportResponse>();

            foreach (var clubId in clubIds)
            {
                result.Add(await BuildClubReportAsync(clubId));
            }

            return result;
        }


        public async Task<bool> IsLeaderOfClubAsync(int leaderAccountId, int clubId)
        {
            return await _context.ClubLeaders
                .AsNoTracking()
                .AnyAsync(cl =>
                    cl.AccountId == leaderAccountId &&
                    cl.ClubId == clubId &&
                    cl.IsActive == true);
        }
    }
}


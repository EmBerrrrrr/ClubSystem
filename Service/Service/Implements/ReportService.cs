using System;
using DTO.DTO.Report;
using Microsoft.EntityFrameworkCore;
using Repository.Models;
using Service.Service.Interfaces;

namespace Service.Service.Implements
{
    /// <summary>
    /// Service cung cấp báo cáo thống kê cho Club Leader.
    /// 
    /// Bảo mật: Kiểm tra leader có quyền quản lý CLB trước khi trả dữ liệu.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly StudentClubManagementContext _context;

        public ReportService(StudentClubManagementContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Xây dựng báo cáo chi tiết cho một CLB.
        /// Được gọi sau khi đã kiểm tra quyền.
        /// </summary>
        private async Task<ClubReportResponse> BuildClubReportAsync(int clubId)
        {
            var club = await _context.Clubs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clubId)
                ?? throw new Exception("Câu lạc bộ không tồn tại.");

            var leader = await _context.ClubLeaders
                .AsNoTracking()
                .Where(cl => cl.ClubId == clubId && cl.IsActive == true)
                .Select(cl => new LeaderInfoDto
                {
                    LeaderName = cl.Account.FullName ?? string.Empty,
                    StartDate = cl.StartDate
                })
                .FirstOrDefaultAsync();

            var totalMembers = await _context.Memberships
                .CountAsync(m => m.ClubId == clubId);

            var activeMembers = await _context.Memberships
                .CountAsync(m => m.ClubId == clubId &&
                             m.Status != null &&
                             m.Status.ToLower() == "active"); // Đã fix lỗi translate trước đó

            // === Tính NewMembers: thành viên mới trong tháng hiện tại ===
            var currentDate = DateTime.Today;
            var startOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var newMembers = await _context.Memberships
                .CountAsync(m => m.ClubId == clubId &&
                                 m.JoinDate.HasValue &&
                                 m.JoinDate.Value >= DateOnly.FromDateTime(startOfMonth) &&
                                 m.JoinDate.Value <= DateOnly.FromDateTime(endOfMonth));

            // === Tính TotalIncome: tổng tiền từ các Payment đã thanh toán thành công ===
            var totalIncome = await _context.Payments
                .Where(p => p.ClubId == clubId &&
                            p.Status != null &&
                            p.Status.ToLower() == "paid")
                .SumAsync(p => p.Amount);

            var activities = await _context.Activities
                .Where(a => a.ClubId == clubId)
                .Select(a => new ActivityReportDto
                {
                    ActivityId = a.Id,
                    Title = a.Title ?? string.Empty,
                    StartTime = a.StartTime,
                    Location = a.Location ?? string.Empty,
                    Participants = a.ActivityParticipants.Count,
                    Status = a.Status ?? string.Empty
                })
                .ToListAsync();

            return new ClubReportResponse
            {
                Club = new ClubInfoDto
                {
                    ClubId = club.Id,
                    ClubName = club.Name ?? string.Empty,
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
                    NewMembers = newMembers,        // Đã tính thực tế
                    TotalActivities = activities.Count,
                    TotalIncome = totalIncome       // Đã tính từ Payment paid
                },
                Activities = activities
            };
        }

        /// <summary>
        /// Lấy báo cáo của một CLB cụ thể (kiểm tra quyền leader trước).
        /// </summary>
        public async Task<ClubReportResponse> GetClubReportAsync(int clubId, int leaderAccountId)
        {
            var isLeader = await IsLeaderOfClubAsync(leaderAccountId, clubId);
            if (!isLeader)
                throw new UnauthorizedAccessException("Bạn không phải là trưởng câu lạc bộ này.");

            return await BuildClubReportAsync(clubId);
        }

        /// <summary>
        /// Lấy báo cáo tất cả CLB mà leader đang quản lý.
        /// </summary>
        public async Task<List<ClubReportResponse>> GetMyClubsReportAsync(int leaderAccountId)
        {
            var clubIds = await _context.ClubLeaders
                .Where(cl => cl.AccountId == leaderAccountId && cl.IsActive == true)
                .Select(cl => cl.ClubId)
                .ToListAsync();

            var reports = new List<ClubReportResponse>();

            foreach (var clubId in clubIds)
            {
                reports.Add(await BuildClubReportAsync(clubId));
            }

            return reports;
        }

        /// <summary>
        /// Kiểm tra một account có phải là leader active của club không.
        /// Được dùng chung ở nhiều nơi.
        /// </summary>
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
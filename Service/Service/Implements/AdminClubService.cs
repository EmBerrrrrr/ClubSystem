using DTO.DTO.Club;
using DTO.DTO.Membership;
using Repository.Repo.Interfaces;
using Service.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    public class AdminClubService : IAdminClubService
    {
        private readonly IClubRepository _clubRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IMembershipRepository _membershipRepo;

        public AdminClubService(
            IClubRepository clubRepo,
            IPaymentRepository paymentRepo,
            IMembershipRepository membershipRepo)
        {
            _clubRepo = clubRepo;
            _paymentRepo = paymentRepo;
            _membershipRepo = membershipRepo;
        }

        // Lấy danh sách CLB để giám sát (trạng thái, số thành viên, tổng doanh thu phí)
        public async Task<List<ClubMonitoringDto>> GetAllClubsForMonitoringAsync()
        {
            var clubs = await _clubRepo.GetAllAsync();
            var result = new List<ClubMonitoringDto>();

            foreach (var club in clubs)
            {
                // Validate: Đảm bảo club có thông tin hợp lệ
                if (club == null || string.IsNullOrEmpty(club.Name))
                    continue;

                // Tính số thành viên active
                var memberCount = await _membershipRepo.GetActiveMemberCountByClubIdAsync(club.Id);

                // Tính tổng doanh thu phí (từ payments đã paid)
                var totalRevenue = await _paymentRepo.GetTotalRevenueFromMembersByClubIdAsync(club.Id);

                result.Add(new ClubMonitoringDto
                {
                    Club = new ClubInfo
                    {
                        Id = club.Id,
                        Name = club.Name,
                        Description = club.Description,
                        Status = club.Status ?? "Unknown",
                        MembershipFee = club.MembershipFee
                    },
                    MemberCount = memberCount,
                    TotalRevenue = totalRevenue
                });
            }

            // Sắp xếp theo tên CLB
            return result.OrderBy(c => c.Club.Name).ToList();
        }

        // Lấy chi tiết CLB để quản lý (bao gồm thông tin membership)
        public async Task<ClubDetailMonitoringDto?> GetClubDetailForMonitoringAsync(int clubId)
        {
            var club = await _clubRepo.GetByIdAsync(clubId);
            
            if (club == null)
                return null;

            // Validate: Đảm bảo club có thông tin hợp lệ
            if (string.IsNullOrEmpty(club.Name))
                throw new Exception("Club không hợp lệ: thiếu tên CLB");

            // Tính số thành viên active
            var memberCount = await _membershipRepo.GetActiveMemberCountByClubIdAsync(club.Id);

            // Tính tổng doanh thu phí (từ payments đã paid)
            var totalRevenue = await _paymentRepo.GetTotalRevenueFromMembersByClubIdAsync(club.Id);

            // Lấy danh sách tất cả members của CLB
            var memberships = await _membershipRepo.GetMembershipsByClubIdAsync(club.Id);
            var members = memberships.Select(m => new ClubMemberDto
            {
                Member = new MemberInfo
                {
                    AccountId = m.AccountId,
                    FullName = m.Account?.FullName,
                    Email = m.Account?.Email,
                    Phone = m.Account?.Phone,
                    Status = m.Status ?? "Unknown"
                },
                MembershipId = m.Id,
                ClubId = m.ClubId,
                JoinDate = m.JoinDate
            }).ToList();

            return new ClubDetailMonitoringDto
            {
                Club = new ClubInfo
                {
                    Id = club.Id,
                    Name = club.Name,
                    Description = club.Description,
                    Status = club.Status ?? "Unknown",
                    MembershipFee = club.MembershipFee
                },
                MemberCount = memberCount,
                TotalRevenue = totalRevenue,
                Members = members
            };
        }
    }
}


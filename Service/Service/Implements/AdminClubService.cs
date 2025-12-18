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
    /// <summary>
    /// Service dành riêng cho Admin để giám sát và quản lý các câu lạc bộ (CLB).
    /// 
    /// Công dụng:
    /// - Cung cấp dữ liệu tổng quan về tất cả CLB (số thành viên active, tổng doanh thu phí).
    /// - Cung cấp chi tiết một CLB cụ thể kèm danh sách thành viên.
    /// 
    /// Luồng từ front-end:
    /// 1. Admin gọi GET /api/admin/clubs → GetAllClubsForMonitoringAsync
    ///    → Lấy toàn bộ Club từ DB → Với mỗi club tính memberCount (active) và totalRevenue (từ payment paid) → Trả list ClubMonitoringDto.
    /// 2. Admin gọi GET /api/admin/clubs/{clubId} → GetClubDetailForMonitoringAsync
    ///    → Lấy chi tiết Club → Tính memberCount + totalRevenue → Lấy tất cả Membership của club → Map thành danh sách member → Trả ClubDetailMonitoringDto.
    /// 
    /// Tương tác giữa các API:
    /// - Dữ liệu được tổng hợp từ bảng Club, Membership (status="active"), Payment (status="paid").
    /// - Không thay đổi dữ liệu DB, chỉ đọc.
    /// - Dùng để admin theo dõi hoạt động tài chính và số lượng thành viên của các CLB.
    /// </summary>
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

        /// <summary>
        /// Lấy danh sách tất cả CLB để admin giám sát (monitoring dashboard).
        /// 
        /// API: GET /api/admin/clubs
        /// Luồng dữ liệu:
        /// - Lấy toàn bộ bản ghi từ bảng Club.
        /// - Với mỗi club: tính số thành viên active (Membership.Status = "active") và tổng doanh thu (sum Payment.Amount với Status = "paid").
        /// - Trả về list ClubMonitoringDto đã sắp xếp theo tên CLB.
        /// </summary>
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

        /// <summary>
        /// Lấy chi tiết một CLB cụ thể để admin quản lý sâu hơn (bao gồm danh sách thành viên).
        /// 
        /// API: GET /api/admin/clubs/{clubId}/detail
        /// Luồng dữ liệu:
        /// - Lấy Club theo Id từ DB.
        /// - Tính memberCount và totalRevenue giống như trên.
        /// - Lấy toàn bộ Membership của club → Map thành danh sách member (với thông tin cá nhân và trạng thái membership).
        /// - Trả về ClubDetailMonitoringDto.
        /// </summary>
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


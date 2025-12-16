using DTO.DTO.Membership;
using System.Collections.Generic;

namespace DTO.DTO.Club
{
    public class ClubDetailMonitoringDto
    {
        // Thông tin CLB (ở root level - frontend có thể bắt theo vị trí này)
        public ClubInfo Club { get; set; } = new ClubInfo();
        
        // Thống kê
        public int MemberCount { get; set; } // Số thành viên active
        public decimal TotalRevenue { get; set; } // Tổng doanh thu phí
        
        // Danh sách thành viên (status của member nằm trong array này)
        public List<ClubMemberDto> Members { get; set; } = new List<ClubMemberDto>();
    }

    public class ClubInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Status { get; set; } // Trạng thái của CLB - frontend bắt status ở đây là club status
        public decimal? MembershipFee { get; set; }

        // Các field mới thêm tương ứng với entity Club
        public DateOnly? EstablishedDate { get; set; }
        public string? ImageClubsUrl { get; set; }
        public string? AvatarPublicId { get; set; }
        public string? Location { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? ActivityFrequency { get; set; }
    }

}


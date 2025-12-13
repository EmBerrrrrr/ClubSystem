namespace DTO.DTO.Club
{
    public class ClubMonitoringDto
    {
        // Thông tin CLB (ở root level - frontend có thể bắt theo vị trí này)
        public ClubInfo Club { get; set; } = new ClubInfo();
        
        // Thống kê
        public int MemberCount { get; set; } // Số thành viên
        public decimal TotalRevenue { get; set; } // Tổng doanh thu phí
    }
}


namespace DTO.DTO.Club
{
    public class ClubDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? EstablishedDate { get; set; }
        public string? ImageClubsUrl { get; set; }
        public string? AvatarPublicId { get; set; }
        public decimal? MembershipFee { get; set; }
        public string? Status { get; set; }
        
        // Thông tin monitoring (chỉ có khi admin xem)
        public int? MemberCount { get; set; } // Số thành viên active
        public decimal? TotalRevenue { get; set; } // Tổng doanh thu phí
    }

}

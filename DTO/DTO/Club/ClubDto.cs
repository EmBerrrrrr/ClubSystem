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
        public string? Location { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? ActivityFrequency { get; set; }

        // Monitoring
        public int? MemberCount { get; set; }
        public decimal? TotalRevenue { get; set; }
    }
}
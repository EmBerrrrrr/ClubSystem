

namespace Service.DTO.ClubLeader
{
    public class MyLeaderRequestDto
    {
        public int Id { get; set; }
        public DateTime RequestDate { get; set; }

        public string Status { get; set; } = "";
        public string? Reason { get; set; }
        public string? Note { get; set; }

        public int? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}


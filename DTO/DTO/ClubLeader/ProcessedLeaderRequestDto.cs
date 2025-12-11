namespace Service.DTO.ClubLeader
{
    public class ProcessedLeaderRequestDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Reason { get; set; } // Lý do student gửi request
        public string? Note { get; set; } // Lý do admin duyệt/từ chối
        public int? ProcessedBy { get; set; }
        public string? ProcessedByUsername { get; set; }
        public string? ProcessedByFullName { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}


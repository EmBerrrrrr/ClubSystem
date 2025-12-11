namespace DTO.DTO.Activity
{
    public class ActivityParticipantDto
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public string ActivityTitle { get; set; } = string.Empty;
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public DateTime? RegisterTime { get; set; }
        public bool? Attended { get; set; } // true = attend, false = cancel, null = chưa xác định
        public string? CancelReason { get; set; } // Lý do hủy (nếu có)
        public string ActivityStatus { get; set; } = string.Empty;
    }
}


namespace DTO.DTO.Activity
{
    public class ActivityParticipantForLeaderDto
    {
        public int ParticipantId { get; set; }
        public int MembershipId { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? RegisterTime { get; set; }
        public bool? Attended { get; set; } // true = attend, false = cancel, null = chưa xác định
        public string? CancelReason { get; set; } // Lý do hủy (nếu có)
    }
}


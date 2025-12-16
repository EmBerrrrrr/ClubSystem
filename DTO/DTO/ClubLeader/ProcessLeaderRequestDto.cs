namespace DTO.DTO.ClubLeader
{
    // Deprecated: kept for backward compatibility
    public class ProcessLeaderRequestDto
    {
        public string? RejectReason { get; set; }
        public string? ApproveNote { get; set; }
    }

    public class ApproveLeaderRequestDto
    {
        public string? AdminNote { get; set; }
    }

    public class RejectLeaderRequestDto
    {
        public string RejectReason { get; set; } = "";
    }
}
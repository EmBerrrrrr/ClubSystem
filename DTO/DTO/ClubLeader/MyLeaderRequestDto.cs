namespace DTO.DTO.ClubLeader
{
    public class MyLeaderRequestDto
    {
        public int Id { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = "";
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Motivation { get; set; }
        public string? Experience { get; set; }
        public string? Vision { get; set; }
        public string? Commitment { get; set; }

        public string? AdminNote { get; set; }
        public string? RejectReason { get; set; }
    }
}
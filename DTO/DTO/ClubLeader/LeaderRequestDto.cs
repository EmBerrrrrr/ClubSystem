namespace Service.DTO.ClubLeader
{
    public class LeaderRequestDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string? Note { get; set; }
        public string? Reason { get; set; }
    }
}
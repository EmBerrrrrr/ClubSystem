namespace Service.DTO.ClubLeader
{
    public class LeaderRequestDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Motivation { get; set; } = "";
        public string Experience { get; set; } = "";
        public string Vision { get; set; } = "";
        public string Commitment { get; set; } ="";
    }

}
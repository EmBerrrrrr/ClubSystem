using DTO.DTO.Club;

namespace DTO.DTO.Membership
{
    public class MyMembershipDto
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public DateOnly? JoinDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public ClubDto? Club { get; set; }
    }
}

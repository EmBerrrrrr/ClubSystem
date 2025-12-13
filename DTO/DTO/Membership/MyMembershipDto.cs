using DTO.DTO.Club;

namespace DTO.DTO.Membership
{
    public class MyMembershipDto
    {
        // Thông tin membership (status nằm trong object này - frontend bắt theo vị trí)
        public MembershipInfo Membership { get; set; } = new MembershipInfo();
        
        // Thông tin CLB (status của CLB nằm trong object này)
        public ClubInfo? Club { get; set; }
    }

    public class MembershipInfo
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public DateOnly? JoinDate { get; set; }
        public string Status { get; set; } = string.Empty; // Trạng thái membership - frontend bắt status ở đây là membership status
    }
}

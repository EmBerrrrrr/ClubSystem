using System;

namespace DTO.DTO.Membership
{
    public class ClubMemberDto
    {
        // Thông tin thành viên (status nằm trong object này - frontend bắt theo vị trí)
        public MemberInfo Member { get; set; } = new MemberInfo();
        
        // Thông tin membership
        public int MembershipId { get; set; }
        public int ClubId { get; set; }
        public DateOnly? JoinDate { get; set; }
    }

    public class MemberInfo
    {
        public int AccountId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = string.Empty; // Trạng thái của thành viên - frontend bắt status ở đây là member status
    }
}


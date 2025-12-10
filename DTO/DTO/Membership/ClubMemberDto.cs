using System;

namespace DTO.DTO.Membership
{
    public class ClubMemberDto
    {
        public int MembershipId { get; set; }
        public int AccountId { get; set; }
        public int ClubId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateOnly? JoinDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}


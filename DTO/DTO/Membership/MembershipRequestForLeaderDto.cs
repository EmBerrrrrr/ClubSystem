using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Membership
{
    public class MembershipRequestForLeaderDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int ClubId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime RequestDate { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Reason { get; set; } // Lý do tham gia (lưu trong Note của request)
        public string? Major { get; set; }
        public string? Skills { get; set; }
    }

}

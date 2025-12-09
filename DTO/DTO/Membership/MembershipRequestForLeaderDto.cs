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
    }

}

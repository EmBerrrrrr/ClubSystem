using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Membership
{
    public class MembershipDTO
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = null!;
        public DateTime? JoinDate { get; set; }
        public string Status { get; set; } = null!;
    }
}

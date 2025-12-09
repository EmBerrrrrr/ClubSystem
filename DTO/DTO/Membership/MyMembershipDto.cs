using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Membership
{
    public class MyMembershipDto
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; }
        public DateOnly? JoinDate { get; set; }
        public string Status { get; set; }
    }


}

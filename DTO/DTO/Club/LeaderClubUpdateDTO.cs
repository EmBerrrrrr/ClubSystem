using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Club
{
    public class LeaderClubUpdateDTO
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime? EstablishedDate { get; set; }
        public string? ImageClubsUrl { get; set; }
        public decimal? MembershipFee { get; set; }
        public string? Status { get; set; }
    }

}

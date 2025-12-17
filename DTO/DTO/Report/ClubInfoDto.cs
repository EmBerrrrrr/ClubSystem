using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Report
{
    public class ClubInfoDto
    {
        public int ClubId { get; set; }
        public required string ClubName { get; set; }
        public string? Description { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? ActivityFrequency { get; set; }
    }
}

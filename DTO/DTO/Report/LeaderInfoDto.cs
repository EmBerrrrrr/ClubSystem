using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Report
{
    public class LeaderInfoDto
    {
        public required string LeaderName { get; set; }
        public DateOnly? StartDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Report
{
    public class ClubStatisticsDto
    {
        public int TotalMembers { get; set; }
        public int ActiveMembers { get; set; }
        public int NewMembers { get; set; }
        public int TotalActivities { get; set; }
        public decimal TotalIncome { get; set; }
    }
}

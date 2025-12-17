using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Report
{
    public class ClubReportResponse
    {
        public required ClubInfoDto Club { get; set; }
        public LeaderInfoDto? Leader { get; set; }
        public required ClubStatisticsDto Statistics { get; set; }
        public required List<ActivityReportDto> Activities { get; set; }
    }
}

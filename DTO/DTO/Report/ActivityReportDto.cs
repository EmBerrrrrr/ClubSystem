using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Report
{
    public class ActivityReportDto
    {
        public int ActivityId { get; set; }
        public required string Title { get; set; }
        public DateTime? StartTime { get; set; }
        public required string Location { get; set; }
        public int Participants { get; set; }
        public required string Status { get; set; }
    }

}

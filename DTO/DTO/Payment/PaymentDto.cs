using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Payment
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int MembershipRequestId { get; set; }
        public int ClubId { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Membership
{
    public class MembershipRequestDto
    {
        public int Id { get; set; }
        public string ClubName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public int? PaymentId { get; set; } // Payment ID nếu đã tạo payment
        public decimal? Amount { get; set; } // Số tiền cần thanh toán (nếu status = approved_pending_payment)
    }

}

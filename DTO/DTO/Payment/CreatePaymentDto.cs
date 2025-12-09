using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.DTO.Payment
{
    public class CreatePaymentDto
    {
        public int MembershipRequestId { get; set; }
        public string Method { get; set; } = string.Empty; // Ví dụ: "cash", "bank_transfer", "momo", etc.
    }
}


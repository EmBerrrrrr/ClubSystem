namespace DTO.DTO.Payment
{
    public class PaymentStatusDto
    {
        public int ClubId { get; set; }
        public string ClubName { get; set; }
        public decimal MembershipFee { get; set; }
        public string PaymentStatus { get; set; } // "paid", "pending", "not_paid", "no_fee"
        public int? PaymentId { get; set; }
        public DateTime? PaidDate { get; set; }
        public bool IsMember { get; set; }
    }
}


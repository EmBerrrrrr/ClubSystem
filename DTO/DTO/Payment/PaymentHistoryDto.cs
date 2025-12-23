namespace DTO.DTO.Payment
{
    public class PaymentHistoryDto
    {
        public int Id { get; set; }
        public int? MembershipId { get; set; }
        public int ClubId { get; set; }
        public string? ClubName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? Method { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public int AccountId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }
}


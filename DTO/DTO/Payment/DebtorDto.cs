namespace DTO.DTO.Payment
{
    public class DebtorDto
    {
        public int? MembershipId { get; set; }
        public int AccountId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int ClubId { get; set; }
        public string? ClubName { get; set; }
        public decimal Amount { get; set; }
        public DateOnly? JoinDate { get; set; }
        public int PaymentId { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? PaymentCreatedDate { get; set; }
    }
}


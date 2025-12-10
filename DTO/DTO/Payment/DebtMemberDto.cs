namespace DTO.DTO.Payment
{
    /// <summary>
    /// DTO để hiển thị member còn nợ phí CLB
    /// </summary>
    public class DebtMemberDto
    {
        public int AccountId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string? MemberEmail { get; set; }
        public string? MemberPhone { get; set; }
        public int MembershipId { get; set; }
        public string MembershipStatus { get; set; } = string.Empty;
        public decimal DebtAmount { get; set; }
        public DateTime? JoinDate { get; set; }
        public int? PendingPaymentId { get; set; }
    }
}


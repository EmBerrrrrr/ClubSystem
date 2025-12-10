namespace DTO.DTO.Payment
{
    /// <summary>
    /// DTO để hiển thị payment của CLB (cho Club Leader)
    /// </summary>
    public class ClubPaymentDto
    {
        public int Id { get; set; }
        public int MembershipId { get; set; }
        public int AccountId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string? MemberEmail { get; set; }
        public string? MemberPhone { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}


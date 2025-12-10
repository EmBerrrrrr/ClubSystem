namespace DTO.DTO.Payment
{
    /// <summary>
    /// DTO để hiển thị payment của một member cụ thể
    /// </summary>
    public class MemberPaymentDto
    {
        public int Id { get; set; }
        public int MembershipId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}


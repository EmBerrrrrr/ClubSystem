namespace DTO.DTO.Payment
{
    // DTO cho trạng thái thanh toán của student (đã đóng)
    public class StudentPaidPaymentDto
    {
        public int Id { get; set; }
        public int MembershipId { get; set; }
        public int ClubId { get; set; }
        public string? ClubName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? Method { get; set; }
        public string? Status { get; set; }
        public long? OrderCode { get; set; }
        public string? Description { get; set; }
    }

    // DTO cho các khoản còn nợ của student
    public class StudentDebtDto
    {
        public int Id { get; set; }
        public int MembershipId { get; set; }
        public int ClubId { get; set; }
        public string? ClubName { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidDate { get; set; }
        public string? Status { get; set; }
        public long? OrderCode { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}


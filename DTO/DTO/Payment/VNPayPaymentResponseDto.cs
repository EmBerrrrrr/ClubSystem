namespace DTO.DTO.Payment
{
    public class VNPayPaymentResponseDto
    {
        public int PaymentId { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
    }
}


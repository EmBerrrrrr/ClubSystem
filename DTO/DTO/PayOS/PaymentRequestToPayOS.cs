namespace DTO.DTO.PayOS
{
    public class PaymentRequestToPayOS
    {
        public long OrderCode { get; set; }
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public required string ReturnUrl { get; set; }
        public required string CancelUrl { get; set; }
    }
}

namespace DTO.DTO.PayOS
{
    public class PaymentLinkRequest
    {
        public long orderCode { get; set; }
        public int amount { get; set; }
        public required string description { get; set; }
        public required string returnUrl { get; set; }
        public required string cancelUrl { get; set; }
    }
}

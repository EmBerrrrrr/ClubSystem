namespace DTO.DTO.PayOS
{
    public class PaymentLinkRequest
    {
        public long orderCode { get; set; }
        public int amount { get; set; }
        public string description { get; set; }
        public string returnUrl { get; set; }
        public string cancelUrl { get; set; }
    }
}

namespace DTO.DTO.PayOS
{
    public class PaymentRequestToPayOS
    {
        public long OrderCode { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
    }
}

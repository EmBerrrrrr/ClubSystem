using DTO.DTO.PayOS;
using Microsoft.Extensions.Configuration;
using Net.payOS;
using Net.payOS.Types;
using Repository.Models;
using Repository.Repo.Interfaces;

namespace Service.Service.Implements
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOS _payOS;
        private readonly IPaymentRepository _paymentRepo;
        private readonly IConfiguration _config;

        public PayOSService(
            PayOS payOS,
            IPaymentRepository paymentRepo,
            IConfiguration config)
        {
            _payOS = payOS;
            _paymentRepo = paymentRepo;
            _config = config;
        }

        public async Task<string> CreatePaymentLink(int paymentId)
        {
            var payment = await _paymentRepo.GetByIdAsync(paymentId);

            if (payment == null)
                throw new Exception("Payment not found");

            long orderCode = long.Parse(DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff"));

            payment.OrderCode = orderCode;
            payment.Method = "PayOS";
            payment.Description = $"Payment for membership {payment.MembershipId}";
            payment.Status = "pending";

            await _paymentRepo.UpdateAsync(payment);

            string returnUrl = $"{_config["Frontend:BaseUrl"]}/pay-success?order={orderCode}";
            string cancelUrl = $"{_config["Frontend:BaseUrl"]}/pay-cancel?order={orderCode}";

            var req = new PaymentLinkRequest()
            {
                orderCode = orderCode,
                amount = (int)payment.Amount,
                description = payment.Description,
                returnUrl = returnUrl,
                cancelUrl = cancelUrl
            };

            CreatePaymentResult result = await _payOS.createPaymentLink(
                new PaymentData(
                    orderCode: req.orderCode,
                    amount: req.amount,
                    description: req.description,
                    items: new List<ItemData> { new ItemData(req.description, 1, req.amount) },
                    cancelUrl: req.cancelUrl,
                    returnUrl: req.returnUrl
                )
            );


            return result.checkoutUrl;
        }


        public async Task HandlePaymentWebhook(WebhookType webhookData)
        {
            WebhookData data = _payOS.verifyPaymentWebhookData(webhookData);

            var payment = await _paymentRepo.GetByOrderCodeAsync(data.orderCode);

            if (payment == null)
                throw new Exception("Payment not found");

            if (data.code == "00")
            {
                payment.Status = "paid";
                payment.PaidDate = DateTime.Now;
            }
            else
            {
                payment.Status = "failed";
            }

            await _paymentRepo.UpdateAsync(payment);
        }
        public async Task<string> ConfirmWebhook(WebhookURL body)
        {
            return await _payOS.confirmWebhook(body.webhook_url);
        }
    }
}

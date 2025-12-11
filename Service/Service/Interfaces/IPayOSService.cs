using DTO.DTO.PayOS;
using Net.payOS.Types;

public interface IPayOSService
{
    Task<string> CreatePaymentLink(int paymentId);
    Task HandlePaymentWebhook(WebhookType webhookData);
    Task<string> ConfirmWebhook(WebhookURL body);
}

using DTO.DTO.PayOS;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;

[Route("api/[controller]")]
[ApiController]
public class PayOSController : ControllerBase
{
    private readonly IPayOSService _service;

    public PayOSController(IPayOSService service)
    {
        _service = service;
    }

    [HttpPost("create/{paymentId}")]
    public async Task<IActionResult> CreateLink(int paymentId)
    {
        var url = await _service.CreatePaymentLink(paymentId);
        return Ok(url);
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] WebhookType data)
    {
        await _service.HandlePaymentWebhook(data);
        return Ok("received");
    }

    [HttpPost("confirm-webhook")]
    public async Task<IActionResult> Confirm([FromBody] WebhookURL url)
    {
        return Ok(await _service.ConfirmWebhook(url));
    }
}

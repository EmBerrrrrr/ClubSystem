using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

public class PaymentHub : Hub
{
    // Client sẽ gọi để join vào group theo paymentId
    public Task JoinPaymentGroup(string paymentId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, $"payment-{paymentId}");
    }

    public Task LeavePaymentGroup(string paymentId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, $"payment-{paymentId}");
    }
}

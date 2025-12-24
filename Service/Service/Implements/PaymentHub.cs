using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Service.Service.Implements
{
    // Legacy hub kept for backward compatibility; not used by the API host.
    public class ServicePaymentHub : Hub
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
}

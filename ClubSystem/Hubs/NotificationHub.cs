using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace ClubSystem.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task Join(int accountId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, accountId.ToString());
        }
    }
}

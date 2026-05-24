using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Laundry.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }

        public async Task SendNotificationToUser(string userId, string title, string message, int orderId)
        {
            await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", title, message, orderId);
        }

        public async Task SendOrderStatusUpdate(string userId, string orderNumber, string status, string message)
        {
            await Clients.Group($"user_{userId}").SendAsync("OrderStatusUpdate", orderNumber, status, message);
        }

        public async Task NotifyAdmins(string title, string message, int orderId)
        {
            await Clients.Group("admins").SendAsync("NewOrderAlert", title, message, orderId);
        }
    }
}
using Laundry.Models;
using Laundry.Patterns.Singleton;
using Laundry.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Laundry.Data;

namespace Laundry.Services
{
    // Driver Notification Class
    public class DriverNotification
    {
        public int DriverId { get; set; }
        public string? DriverPhone { get; set; }
        public string Message { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
    }

    // Staff Notification Class
    public class StaffNotification
    {
        public string StaffName { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class NotificationService
    {
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _context;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            ApplicationDbContext context)
        {
            _emailService = new EmailService();
            _smsService = new SmsService();
            _hubContext = hubContext;
            _context = context;
        }

        // Send order confirmation (Email, SMS, In-App)
        public async Task SendOrderConfirmationAsync(User user, Order order)
        {
            // 1. Send Email
            var subject = $"Order Confirmation - #{order.OrderNumber}";
            var body = $@"
                <h2>Order Confirmation</h2>
                <p>Dear {user.FullName},</p>
                <p>Your order #{order.OrderNumber} has been confirmed.</p>
                <p><strong>Order Details:</strong></p>
                <ul>
                    <li>Service: {order.ServiceType}</li>
                    <li>Package: {order.PackageTier}</li>
                    <li>Total: {order.TotalAmount:C}</li>
                    <li>Estimated Pickup: {order.OrderDate.AddHours(24):dd MMM yyyy}</li>
                </ul>
                <p>We'll notify you when a driver is assigned.</p>
            ";
            await _emailService.SendAsync(user.Email, subject, body);

            // 2. Send SMS
            await _smsService.SendAsync(user.PhoneNumber, $"Order #{order.OrderNumber} confirmed! Total: {order.TotalAmount:C}");

            // 3. Send In-App Notification (SignalR)
            await SendInAppNotification(user.UserId, "Order Confirmed", $"Your order #{order.OrderNumber} has been confirmed!", order.OrderId);

            // 4. Save to database
            await SaveNotification(user.UserId, "Order Confirmed", $"Your order #{order.OrderNumber} has been confirmed!", "OrderConfirmation", order.OrderId);

            OrderLogger.Instance.LogEvent($"Order confirmation sent to {user.Email}");
        }

        // ========== METHODS CALLED BY OrderService ==========

        // Send status update - THIS WAS MISSING
        public async Task SendStatusUpdateAsync(User user, Order order)
        {
            var statusMessages = new Dictionary<OrderStatus, (string Subject, string EmailBody, string SmsMessage, string Title)>
            {
                [OrderStatus.PickupAssigned] = (
                    "Driver Assigned for Pickup",
                    $"<p>A driver has been assigned to pick up your laundry. Driver: {order.DriverName}, Phone: {order.DriverPhone}</p>",
                    $"Driver {order.DriverName} assigned for pickup. Phone: {order.DriverPhone}",
                    "Driver Assigned"
                ),
                [OrderStatus.DriverEnRoute] = (
                    "Driver is on the way",
                    $"<p>Your driver is en route to pick up your laundry. ETA: {order.EtaMinutes} minutes.</p>",
                    $"Driver en route. ETA: {order.EtaMinutes} minutes.",
                    "Driver En Route"
                ),
                [OrderStatus.DriverNearby] = (
                    "Driver is nearby",
                    "<p>Your driver is approximately 5 minutes away.</p>",
                    "Driver is 5 minutes away!",
                    "Driver Nearby"
                ),
                [OrderStatus.DriverArrived] = (
                    "Driver has arrived",
                    "<p>The driver has arrived at your location for pickup.</p>",
                    "Driver has arrived for pickup.",
                    "Driver Arrived"
                ),
                [OrderStatus.PickedUp] = (
                    "Laundry Picked Up",
                    "<p>Your laundry has been picked up and is on its way to our facility.</p>",
                    "Laundry picked up successfully.",
                    "Laundry Picked Up"
                ),
                [OrderStatus.WashingInProgress] = (
                    "Washing in Progress",
                    $"<p>Your laundry is being processed. Staff: {order.AssignedStaffName}</p>",
                    "Washing in progress.",
                    "Washing Started"
                ),
                [OrderStatus.OutForDelivery] = (
                    "Out for Delivery",
                    $"<p>Your order is out for delivery. Driver: {order.DriverName}</p>",
                    $"Order out for delivery. Driver: {order.DriverName}",
                    "Out for Delivery"
                ),
                [OrderStatus.Delivered] = (
                    "Order Delivered",
                    "<p>Your order has been delivered. Thank you for using our service!</p>",
                    "Order delivered! Thank you for choosing us.",
                    "Order Delivered"
                )
            };

            if (statusMessages.TryGetValue(order.Status, out var message))
            {
                // 1. Send Email
                await _emailService.SendAsync(user.Email, message.Subject, message.EmailBody);

                // 2. Send SMS
                await _smsService.SendAsync(user.PhoneNumber, message.SmsMessage);

                // 3. Send In-App Notification (SignalR)
                await SendInAppNotification(user.UserId, message.Title, message.SmsMessage, order.OrderId);

                // 4. Save to database
                await SaveNotification(user.UserId, message.Title, message.SmsMessage, "StatusUpdate", order.OrderId);

                OrderLogger.Instance.LogEvent($"Status update sent to {user.Email}: {order.Status}");
            }
        }

        // Send driver notification - THIS WAS MISSING
        public async Task SendDriverNotificationAsync(DriverNotification notification)
        {
            var message = $"[Driver {notification.DriverId}] {notification.Message}";
            if (!string.IsNullOrEmpty(notification.DriverPhone))
            {
                await _smsService.SendAsync(notification.DriverPhone, message);
            }

            // In-app notification for driver (if drivers have accounts)
            if (notification.DriverId > 0)
            {
                await SendInAppNotification(notification.DriverId, "Driver Alert", notification.Message, notification.OrderId);
            }

            OrderLogger.Instance.LogEvent($"Driver notification: {message}");
        }

        // Send staff notification - THIS WAS MISSING
        public async Task SendStaffNotificationAsync(StaffNotification notification)
        {
            var message = $"[Staff {notification.StaffName}] Order #{notification.OrderNumber}: {notification.Message}";

            // In-app notification for staff
            await _hubContext.Clients.Group("staff").SendAsync("StaffNotification", notification.StaffName, notification.Message, notification.OrderNumber);

            OrderLogger.Instance.LogEvent($"Staff notification: {message}");
        }

        // ========== HELPER METHODS ==========

        // In-App Notification via SignalR
        private async Task SendInAppNotification(int userId, string title, string message, int? orderId = null)
        {
            try
            {
                await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", title, message, orderId);
                OrderLogger.Instance.LogEvent($"In-app notification sent to user {userId}: {title}");
            }
            catch (Exception ex)
            {
                OrderLogger.Instance.LogEvent($"Failed to send in-app notification: {ex.Message}", EventLevel.Error);
            }
        }

        // Save notification to database
        private async Task SaveNotification(int userId, string title, string message, string type, int? orderId = null)
        {
            var notification = new Models.Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now,
                OrderId = orderId
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // Get unread notifications count
        public async Task<int> GetUnreadCount(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
        }

        // Get user notifications
        public async Task<List<Models.Notification>> GetUserNotifications(int userId, int take = 50)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        // Mark notification as read
        public async Task MarkAsRead(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        // Mark all as read
        public async Task MarkAllAsRead(int userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        // New Order Alert for Admins
        public async Task NotifyAdminsAsync(string title, string message, int orderId)
        {
            await _hubContext.Clients.Group("admins").SendAsync("NewOrderAlert", title, message, orderId);
            OrderLogger.Instance.LogEvent($"Admin notification: {title} - {message}");
        }
    }

    // Email Service (Simulated)
    public class EmailService
    {
        public async Task SendAsync(string to, string subject, string body)
        {
            await Task.Delay(100);
            Console.WriteLine($"[EMAIL] To: {to}, Subject: {subject}");
            Console.WriteLine($"[EMAIL] Body: {body}");
            Console.WriteLine("---");
        }
    }

    // SMS Service (Simulated)
    public class SmsService
    {
        public async Task SendAsync(string to, string message)
        {
            await Task.Delay(50);
            Console.WriteLine($"[SMS] To: {to}, Message: {message}");
            Console.WriteLine("---");
        }
    }
}
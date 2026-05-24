using Laundry.Models;
namespace Laundry.Patterns
{
    // Observer Pattern - Real-time notifications for order status changes
    public interface IOrderObserver
    {
        string Name { get; }
        string Role { get; }
        void OnOrderStatusChanged(int orderId, string orderNumber, OrderStatus oldStatus, OrderStatus newStatus);
        List<string> GetNotificationHistory();
    }

    public class CustomerObserver : IOrderObserver
    {
        private readonly string _customerName;
        private readonly string _customerEmail;
        private readonly string _customerPhone;
        private readonly List<string> _notifications = new();

        public CustomerObserver(string name, string email, string phone)
        {
            _customerName = name;
            _customerEmail = email;
            _customerPhone = phone;
        }

        public string Name => _customerName;
        public string Role => "Customer";

        public void OnOrderStatusChanged(int orderId, string orderNumber, OrderStatus oldStatus, OrderStatus newStatus)
        {
            var message = GetCustomerMessage(orderNumber, newStatus);
            _notifications.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

            // Simulate SMS/Email notification (Bonus)
            SimulateNotification(message);
        }

        private string GetCustomerMessage(string orderNumber, OrderStatus status) => status switch
        {
            OrderStatus.PaymentConfirmed => $"✅ Order #{orderNumber}: Payment confirmed! We're preparing your laundry pickup.",
            OrderStatus.PickupAssigned => $"🚗 Order #{orderNumber}: A driver has been assigned for pickup.",
            OrderStatus.DriverEnRoute => $"🚚 Order #{orderNumber}: Driver is on the way to your location!",
            OrderStatus.PickedUp => $"📦 Order #{orderNumber}: Your laundry has been picked up!",
            OrderStatus.AtLaundry => $"🧺 Order #{orderNumber}: Your laundry has arrived at our facility.",
            OrderStatus.WashingInProgress => $"💧 Order #{orderNumber}: We're now washing your items!",
            OrderStatus.Completed => $"✨ Order #{orderNumber}: Your laundry is complete and ready!",
            OrderStatus.OutForDelivery => $"🚛 Order #{orderNumber}: Your order is out for delivery!",
            OrderStatus.Delivered => $"🏠 Order #{orderNumber}: Delivered! Thank you for using FreshDrop!",
            OrderStatus.ReadyForPickup => $"📦 Order #{orderNumber}: Ready for pickup at our facility!",
            _ => $"Order #{orderNumber}: Status updated to {status}"
        };

        private void SimulateNotification(string message)
        {
            // Simulate SMS
            Console.WriteLine($"[SMS to {_customerPhone}] {message}");
            // Simulate Email
            Console.WriteLine($"[EMAIL to {_customerEmail}] {message}");
        }

        public List<string> GetNotificationHistory() => _notifications.ToList();
    }

    public class DriverObserver : IOrderObserver
    {
        private readonly string _driverName;
        private readonly string _driverPhone;
        private readonly List<string> _notifications = new();

        public DriverObserver(string name, string phone)
        {
            _driverName = name;
            _driverPhone = phone;
        }

        public string Name => _driverName;
        public string Role => "Driver";

        public void OnOrderStatusChanged(int orderId, string orderNumber, OrderStatus oldStatus, OrderStatus newStatus)
        {
            if (newStatus == OrderStatus.PickupAssigned || newStatus == OrderStatus.OutForDelivery)
            {
                var message = $"[DRIVER {_driverName}] Order #{orderNumber}: {GetDriverMessage(newStatus)}";
                _notifications.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                Console.WriteLine($"[SMS to {_driverPhone}] {message}");
            }
        }

        private string GetDriverMessage(OrderStatus status) => status switch
        {
            OrderStatus.PickupAssigned => "New pickup assignment! Check app for customer details.",
            OrderStatus.OutForDelivery => "Delivery required! Customer address available in app.",
            _ => $"Status changed to {status}"
        };

        public List<string> GetNotificationHistory() => _notifications.ToList();
    }

    public class LaundryStaffObserver : IOrderObserver
    {
        private readonly string _staffName;
        private readonly List<string> _notifications = new();

        public LaundryStaffObserver(string name)
        {
            _staffName = name;
        }

        public string Name => _staffName;
        public string Role => "Laundry Staff";

        public void OnOrderStatusChanged(int orderId, string orderNumber, OrderStatus oldStatus, OrderStatus newStatus)
        {
            if (newStatus == OrderStatus.AtLaundry || newStatus == OrderStatus.WashingInProgress || newStatus == OrderStatus.Completed)
            {
                var message = $"[STAFF {_staffName}] Order #{orderNumber}: {GetStaffMessage(newStatus)}";
                _notifications.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
                Console.WriteLine(message);
            }
        }

        private string GetStaffMessage(OrderStatus status) => status switch
        {
            OrderStatus.AtLaundry => "Order received at facility. Ready for processing queue.",
            OrderStatus.WashingInProgress => "Started washing the laundry items.",
            OrderStatus.Completed => "Order completed! Ready for quality check and packaging.",
            _ => $"Status: {status}"
        };

        public List<string> GetNotificationHistory() => _notifications.ToList();
    }

    public class AdminObserver : IOrderObserver
    {
        private readonly List<string> _notifications = new();

        public string Name => "Admin System";
        public string Role => "Administrator";

        public void OnOrderStatusChanged(int orderId, string orderNumber, OrderStatus oldStatus, OrderStatus newStatus)
        {
            var message = $"[ADMIN LOG] Order #{orderNumber}: {oldStatus} → {newStatus} at {DateTime.Now:HH:mm:ss}";
            _notifications.Add(message);

            // Log to singleton
            Singleton.OrderLogger.Instance.LogEvent(message);
        }

        public List<string> GetNotificationHistory() => _notifications.ToList();
    }

    public class OrderStatusManager
    {
        private readonly List<IOrderObserver> _observers = new();

        public void Attach(IOrderObserver observer) => _observers.Add(observer);
        public void Detach(IOrderObserver observer) => _observers.Remove(observer);
        public List<IOrderObserver> GetObservers() => _observers.ToList();

        public void UpdateStatus(int orderId, string orderNumber, OrderStatus oldStatus, OrderStatus newStatus)
        {
            foreach (var observer in _observers)
            {
                observer.OnOrderStatusChanged(orderId, orderNumber, oldStatus, newStatus);
            }
        }
    }
}
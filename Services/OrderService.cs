using Laundry.Data;
using Laundry.Models;
using Laundry.Models.ViewModels;
using Laundry.Patterns;
using Laundry.Patterns.Singleton;
using Laundry.Patterns.AbstractFactory;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Laundry.Services
{
    public class OrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly LoyaltyService _loyaltyService;
        private readonly RouteOptimizationService _routeService;
        private static readonly Dictionary<int, OrderStatusManager> _statusManagers = new();

        public OrderService(
            ApplicationDbContext context,
            NotificationService notificationService,
            LoyaltyService loyaltyService,
            RouteOptimizationService routeService)
        {
            _context = context;
            _notificationService = notificationService;
            _loyaltyService = loyaltyService;
            _routeService = routeService;
        }

        public async Task<int> GetTotalOrderCount()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> GetPendingOrderCount()
        {
            return await _context.Orders.CountAsync(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);
        }

        // Add a method to periodically check payment status (can be called by a background job)
        public async Task CheckPendingPayments()
        {
            var pendingOrders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Pending && o.OrderDate < DateTime.Now.AddHours(-24))
                .ToListAsync();

            foreach (var order in pendingOrders)
            {
                // Check EFT status
                var eftPayment = await _context.PaymentInstructions
                    .FirstOrDefaultAsync(p => p.OrderId == order.OrderId && p.IsConfirmed);

                if (eftPayment != null)
                {
                    order.Status = OrderStatus.PaymentConfirmed;
                }
                else
                {
                    // Cancel order after 24 hours
                    order.Status = OrderStatus.Cancelled;
                    OrderLogger.Instance.LogEvent($"Order {order.OrderNumber} cancelled - payment not received within 24 hours");
                }
            }

            await _context.SaveChangesAsync();
        }
        public async Task<decimal> GetTotalRevenue()
        {
            return await _context.Orders.SumAsync(o => o.TotalAmount);
        }

        public OrderStatusManager GetStatusManager(int orderId)
        {
            if (!_statusManagers.ContainsKey(orderId))
                _statusManagers[orderId] = new OrderStatusManager();
            return _statusManagers[orderId];
        }

        public async Task<(bool Success, string Message, Order? Order)> CreateOrderAsync(int userId, CreateOrderViewModel model)
        {
            var user = await _context.Users
                .Include(u => u.Subscription)
                .Include(u => u.LoyaltyRewards)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return (false, "User not found.", null);

            // 1. Factory Method Pattern - Create service
            var serviceFactory = new LaundryServiceFactory();
            ILaundryService service = serviceFactory.CreateService(model.SelectedService);
            OrderLogger.Instance.LogEvent($"Factory Method: Created {service.Name} for user {user.Email}");

            // 2. Abstract Factory Pattern - Get package based on subscription
            string packageTier = user.Subscription?.Tier.ToString() ?? "Standard";
            var packageFactory = PackageFactoryResolver.GetFactory(packageTier.ToLower());

            // Check if service is available for this package
            if (!packageFactory.AvailableServices.Contains(model.SelectedService))
            {
                return (false, $"Service '{service.Name}' is not available with your {packageTier} subscription. Please upgrade your plan.", null);
            }

            var packaging = packageFactory.CreatePackaging();
            var delivery = packageFactory.CreateDeliveryMethod();
            var detergent = packageFactory.CreateDetergentType();

            OrderLogger.Instance.LogEvent($"Abstract Factory: Using {packageTier} package with {packaging.Type}, {delivery.Name}, {detergent.Name}");

            // 3. Decorator Pattern - Add extras
            OrderComponent orderComponent = new BaseLaundryOrder(service);
            foreach (var extra in model.SelectedExtras)
            {
                orderComponent = extra.ToLower() switch
                {
                    "fabricsoftener" => new FabricSoftenerDecorator(orderComponent),
                    "stainremoval" => new StainRemovalDecorator(orderComponent),
                    "perfume" => new PerfumeTreatmentDecorator(orderComponent),
                    "delicate" => new DelicateWashDecorator(orderComponent),
                    "extrafolding" => new ExtraFoldingDecorator(orderComponent),
                    "hanger" => new HangerPackagingDecorator(orderComponent),
                    _ => orderComponent
                };
            }

            OrderLogger.Instance.LogEvent($"Decorator: Extras added - {orderComponent.GetExtrasDescription()}");

            // Calculate base cost
            decimal subtotal = orderComponent.CalculateCost() + packaging.Cost + delivery.Cost + detergent.Premium;

            // Apply weight-based pricing
            if (model.WeightKg > 5)
            {
                decimal extraWeightFee = (model.WeightKg - 5) * 5.00m;
                subtotal += extraWeightFee;
            }

            // 4. Strategy Pattern - Apply payment/discounts
            var paymentContext = new PaymentContext();
            Dictionary<string, string> paymentDetails = new();

            // Apply subscription discount first
            decimal subscriptionDiscount = user.Subscription?.DiscountRate ?? 0m;
            if (subscriptionDiscount > 0)
            {
                paymentContext.SetStrategy(new SubscriptionDiscountStrategy(subscriptionDiscount));
                subtotal = paymentContext.CalculateTotal(subtotal);
            }

            // Apply loyalty discount
            decimal loyaltyDiscount = user.LoyaltyRewards?.GetDiscountRate() ?? 0m;
            if (loyaltyDiscount > 0)
            {
                paymentContext.SetStrategy(new LoyaltyDiscountStrategy(loyaltyDiscount));
                subtotal = paymentContext.CalculateTotal(subtotal);
            }

            // Apply promo code if provided
            decimal promoDiscount = 0;
            PromoCode? promo = null;
            if (!string.IsNullOrEmpty(model.PromoCode))
            {
                promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code.ToUpper() == model.PromoCode.ToUpper());
                if (promo != null && promo.IsValidForUser(userId))
                {
                    paymentContext.SetStrategy(new PromoCodeStrategy(promo.Code, promo.DiscountPercent));
                    subtotal = paymentContext.CalculateTotal(subtotal);
                    promoDiscount = promo.DiscountPercent;
                }
                else if (promo != null)
                {
                    return (false, "This promo code has expired or has already been used.", null);
                }
            }

            // Apply final payment method
            switch (model.SelectedPayment.ToLower())
            {
                case "creditcard":
                    paymentContext.SetStrategy(new CreditCardStrategy());
                    paymentDetails["CardNumber"] = model.CardNumber ?? "";
                    paymentDetails["Expiry"] = model.CardExpiry ?? "";
                    paymentDetails["Cvv"] = model.CardCvv ?? "";
                    break;
                case "eft":
                    paymentContext.SetStrategy(new EFTStrategy());
                    paymentDetails["BankReference"] = model.BankReference ?? Guid.NewGuid().ToString().Substring(0, 8);
                    break;
                case "payfast":
                    paymentContext.SetStrategy(new PayFastStrategy());
                    paymentDetails["PayFastEmail"] = model.PayFastEmail ?? user.Email;
                    break;
                default:
                    paymentContext.SetStrategy(new CreditCardStrategy());
                    break;
            }

            if (!paymentContext.Validate(paymentDetails))
                return (false, "Invalid payment details. Please check and try again.", null);

            decimal totalAmount = paymentContext.CalculateTotal(subtotal);
            string paymentResult = paymentContext.Process(totalAmount, paymentDetails);

            OrderLogger.Instance.LogEvent($"Strategy: {paymentContext.GetStrategyName()} applied. Total: {totalAmount:C}");

            // Create order
            var order = new Order
            {
                UserId = userId,
                ServiceType = service.Name,
                ServiceBasePrice = service.BasePrice,
                PackageTier = packageTier,
                PackagingType = packaging.Type,
                DeliveryMethod = delivery.Name,
                DetergentType = detergent.Name,
                Extras = JsonConvert.SerializeObject(model.SelectedExtras),
                PaymentMethod = model.SelectedPayment,
                PromoCodeUsed = model.PromoCode ?? "",
                Subtotal = orderComponent.CalculateCost(),
                DiscountAmount = subtotal - totalAmount,
                TotalAmount = totalAmount,
                OrderType = model.OrderType,
                PickupAddress = model.OrderType == OrderType.Pickup ? user.GetFullAddress() : model.PickupAddress,
                DeliveryAddress = model.OrderType == OrderType.Delivery ? model.DeliveryAddress : user.GetFullAddress(),
                WeightKg = model.WeightKg,
                Status = OrderStatus.PaymentConfirmed,
                OrderDate = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Mark promo code as used
            if (promo != null)
            {
                promo.MarkUsed(userId);
                await _context.SaveChangesAsync();
            }



            // 5. Observer Pattern - Attach observers
            var statusManager = GetStatusManager(order.OrderId);
            statusManager.Attach(new CustomerObserver(user.FullName, user.Email, user.PhoneNumber));
            statusManager.Attach(new DriverObserver($"Driver_{new Random().Next(1000, 9999)}", "555-0123"));
            statusManager.Attach(new LaundryStaffObserver("Staff_John"));
            statusManager.Attach(new AdminObserver());

            // Initial status update
            statusManager.UpdateStatus(order.OrderId, order.OrderNumber, OrderStatus.Pending, OrderStatus.PaymentConfirmed);

            // 6. Singleton - Log creation
            OrderLogger.Instance.LogEvent($"Order Created: #{order.OrderNumber} | User: {user.Email} | Total: {totalAmount:C}");

            // Send notifications
            await _notificationService.SendOrderConfirmationAsync(user, order);

            // Update loyalty points
            await _loyaltyService.AddPointsAsync(userId, (int)(totalAmount / 10)); // 10 points per R10 spent

            return (true, $"Order created successfully! {paymentResult}", order);
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, int? staffId = null, string? staffName = null)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return;

            var oldStatus = order.Status;
            order.Status = newStatus;

            // Update based on status
            switch (newStatus)
            {
                case OrderStatus.PickupAssigned:
                    var driverId = new Random().Next(100, 999);
                    order.AssignedDriverId = driverId;
                    order.DriverName = $"Driver_{driverId}";
                    order.DriverPhone = $"555-{driverId}";
                    break;

                case OrderStatus.DriverEnRoute:
                    order.EtaMinutes = 30;
                    var route = await _routeService.OptimizeDeliveryRouteAsync(order.PickupAddress, order.DeliveryAddress, order.AssignedDriverId);
                    order.EtaMinutes = (int)route.EstimatedDuration.TotalMinutes;
                    break;

                case OrderStatus.DriverNearby:
                    order.EtaMinutes = 5;
                    order.DriverProximityStatus = "5 minutes away";
                    break;

                case OrderStatus.DriverArrived:
                    order.EtaMinutes = 0;
                    order.DriverProximityStatus = "Arrived";
                    break;

                case OrderStatus.PickedUp:
                    order.PickupDate = DateTime.Now;
                    break;

                case OrderStatus.WashingInProgress:
                    order.AssignedStaffId = staffId;
                    order.AssignedStaffName = staffName;
                    break;

                case OrderStatus.OutForDelivery:
                    var deliveryRoute = await _routeService.OptimizeDeliveryRouteAsync(order.PickupAddress, order.DeliveryAddress);
                    order.EtaMinutes = (int)deliveryRoute.EstimatedDuration.TotalMinutes;
                    break;

                case OrderStatus.Delivered:
                    order.DeliveryDate = DateTime.Now;
                    break;
            }

            await _context.SaveChangesAsync();

            // Log status change
            OrderLogger.Instance.LogEvent($"Order #{order.OrderNumber} status updated: {oldStatus} → {newStatus} by staff: {staffName ?? "System"}");

            // Send notifications based on status
            await SendStatusNotificationsAsync(order, oldStatus, newStatus);
        }

        private async Task SendStatusNotificationsAsync(Order order, OrderStatus oldStatus, OrderStatus newStatus)
        {
            var user = order.User;
            if (user == null) return;

            // Notify customer
            await _notificationService.SendStatusUpdateAsync(user, order);

            // Notify driver for certain statuses
            if (newStatus == OrderStatus.PickupAssigned && order.AssignedDriverId.HasValue)
            {
                await _notificationService.SendDriverNotificationAsync(new DriverNotification
                {
                    DriverId = order.AssignedDriverId.Value,
                    DriverPhone = order.DriverPhone,
                    Message = $"New pickup assigned: Order #{order.OrderNumber} at {order.PickupAddress}",
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber
                });
            }

            // Notify staff
            if ((newStatus == OrderStatus.WashingInProgress || newStatus == OrderStatus.ReadyForPickup) && !string.IsNullOrEmpty(order.AssignedStaffName))
            {
                await _notificationService.SendStaffNotificationAsync(new StaffNotification
                {
                    StaffName = order.AssignedStaffName,
                    OrderId = order.OrderId,
                    OrderNumber = order.OrderNumber,
                    Message = $"Order #{order.OrderNumber} is ready for processing"
                });
            }
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetPendingOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.OrderDate)
                .ToListAsync();
        }
    }
}
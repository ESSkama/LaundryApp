using Laundry.Data;
using Laundry.Models;
using Laundry.Models.ViewModels;
using Laundry.Patterns.Singleton;
using Laundry.Patterns.AbstractFactory;
using Laundry.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;

namespace Laundry.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;
        private readonly SubscriptionService _subscriptionService;
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly LoyaltyService _loyaltyService;
        private readonly PaymentService _paymentService;

        public OrderController(
            OrderService orderService,
            SubscriptionService subscriptionService,
            ApplicationDbContext context,
            NotificationService notificationService,
            LoyaltyService loyaltyService,
            PaymentService paymentService)
        {
            _orderService = orderService;
            _subscriptionService = subscriptionService;
            _context = context;
            _notificationService = notificationService;
            _loyaltyService = loyaltyService;
            _paymentService = paymentService;
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var subscription = await _subscriptionService.GetSubscriptionAsync(userId);
            var currentTier = subscription?.Tier ?? SubscriptionTier.Standard;

            // Get subscription benefits
            var freeDeliveriesRemaining = await _subscriptionService.GetRemainingFreeDeliveries(userId);
            var deliveriesUsed = await _subscriptionService.GetDeliveriesUsedThisMonth(userId);
            var discountRate = subscription?.DiscountRate ?? PricingConfig.Subscriptions.GetDiscountRate(currentTier);
            var totalFreeDeliveries = subscription?.FreeDeliveriesPerMonth ?? PricingConfig.Subscriptions.GetFreeDeliveries(currentTier);
            var hasActiveSubscription = subscription?.IsActive ?? false;

            // Prepare the data to pass to JavaScript
            var subscriptionData = new
            {
                success = true,
                hasSubscription = hasActiveSubscription,
                tier = currentTier.ToString(),
                discountRate = discountRate,
                freeDeliveriesRemaining = freeDeliveriesRemaining,
                totalFreeDeliveries = totalFreeDeliveries,
                message = hasActiveSubscription ? $"Active {currentTier} plan" : "No active subscription"
            };

            ViewBag.SubscriptionData = Newtonsoft.Json.JsonConvert.SerializeObject(subscriptionData);
            ViewBag.CurrentTier = currentTier.ToString();

            var model = new CreateOrderViewModel
            {
                OrderType = OrderType.Delivery,
                WeightKg = PricingConfig.BaseWeightKg,
                AvailableServices = GetAvailableServices(currentTier),
                AvailableExtras = GetAvailableExtras(),
                PaymentMethods = GetPaymentMethods(),
                SelectedExtras = new List<string>(),

                CurrentSubscriptionTier = currentTier,
                HasActiveSubscription = hasActiveSubscription,
                SubscriptionDiscountRate = discountRate,
                FreeDeliveriesRemaining = freeDeliveriesRemaining,
                TotalFreeDeliveries = totalFreeDeliveries,
                DeliveriesUsedThisMonth = deliveriesUsed,

                Subtotal = 0,
                DiscountAmount = 0,
                DeliveryFee = PricingConfig.Delivery.Standard,
                TotalAmount = 0,
                FreeDeliveryApplied = false,
                SelectedPackage = currentTier.ToString()
            };

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (model.SelectedServices == null || !model.SelectedServices.Any())
            {
                ModelState.AddModelError("", "Please select at least one service");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.SelectedPayment))
            {
                ModelState.AddModelError("", "Please select a payment method");
                return View(model);
            }

            var subscription = await _subscriptionService.GetSubscriptionAsync(userId);
            var hasSubscription = subscription?.IsActive ?? false;
            var discountRate = subscription?.DiscountRate ?? PricingConfig.Subscriptions.GetDiscountRate(SubscriptionTier.Standard);
            var subscriptionTier = subscription?.Tier ?? SubscriptionTier.Standard;

            var (packagingName, packagingFee) = GetPackagingByTier(subscriptionTier);
            var (deliveryName, deliveryFee) = GetDeliveryByTier(subscriptionTier);
            var (detergentName, detergentFee) = GetDetergentByTier(subscriptionTier);

            // Calculate selected services total
            // Calculate selected services total
            decimal selectedServicesTotal = 0;
            var serviceNamesList = new List<string>();

            // DEBUG: Log selected services
            System.Diagnostics.Debug.WriteLine("=== SELECTED SERVICES DEBUG ===");
            foreach (var service in model.SelectedServices)
            {
                var price = GetBaseServicePrice(service);
                System.Diagnostics.Debug.WriteLine($"Service: '{service}', Price: {price}");
                selectedServicesTotal += price;
                serviceNamesList.Add(GetServiceDisplayName(service));
            }
            System.Diagnostics.Debug.WriteLine($"Total Services Price: {selectedServicesTotal}");
            System.Diagnostics.Debug.WriteLine($"Service Names: {string.Join(", ", serviceNamesList)}");

            var weightFee = model.WeightKg > PricingConfig.BaseWeightKg
                ? (model.WeightKg - PricingConfig.BaseWeightKg) * PricingConfig.ExtraWeightRate
                : 0;
            var extrasTotal = CalculateExtrasTotal(model.SelectedExtras);
            var extrasNamesList = model.SelectedExtras?.Select(e => GetExtraDisplayName(e)).ToList() ?? new List<string>();

            // Calculate subtotal (all charges before discount)
            var subtotal = selectedServicesTotal + weightFee + extrasTotal + packagingFee + detergentFee;

            // Apply free delivery if available
            var finalDeliveryFee = deliveryFee;
            var freeDeliveryApplied = false;

            if (hasSubscription && deliveryFee > 0)
            {
                var canUseFreeDelivery = await _subscriptionService.CanUseFreeDelivery(userId);
                if (canUseFreeDelivery)
                {
                    finalDeliveryFee = 0;
                    freeDeliveryApplied = true;
                }
            }

            // Add delivery fee to subtotal
            var totalWithDelivery = subtotal + finalDeliveryFee;

            // Apply subscription discount
            var discountAmount = hasSubscription ? totalWithDelivery * discountRate : 0;
            var total = totalWithDelivery - discountAmount;

            // Apply promo code if provided
            decimal promoDiscountAmount = 0;
            PromoCode? promo = null;
            if (!string.IsNullOrEmpty(model.PromoCode))
            {
                promo = await _context.PromoCodes.FirstOrDefaultAsync(p => p.Code.ToUpper() == model.PromoCode.ToUpper() && p.IsActive);
                if (promo != null && promo.IsValidForUser(userId))
                {
                    promoDiscountAmount = total * (promo.DiscountPercent / 100);
                    total -= promoDiscountAmount;
                    discountAmount += promoDiscountAmount;
                }
            }

            // Create the order with correct values
            var order = new Order
            {
                UserId = userId,
                OrderNumber = GenerateOrderNumber(),
                OrderDate = DateTime.Now,
                OrderType = OrderType.Delivery,
                ServiceType = string.Join(", ", serviceNamesList),
                ServiceBasePrice = selectedServicesTotal,  // Sum of all selected services
                PackageTier = subscriptionTier.ToString(),
                PackagingType = packagingName,
                DeliveryMethod = deliveryName,
                DetergentType = detergentName,
                SelectedExtras = extrasNamesList.Any() ? string.Join(", ", extrasNamesList) : null,
                PaymentMethod = model.SelectedPayment,
                PromoCodeUsed = model.PromoCode,
                Subtotal = subtotal,  // Total without delivery fee (services + weight + extras + packaging + detergent)
                DiscountAmount = discountAmount,
                DeliveryFee = finalDeliveryFee,
                TotalAmount = total,
                PickupAddress = model.PickupAddress,
                DeliveryAddress = model.DeliveryAddress,
                WeightKg = model.WeightKg,
                FreeDeliveryApplied = freeDeliveryApplied,
                SubscriptionTierAtOrder = subscriptionTier,
                DiscountRateApplied = discountRate,
                Status = OrderStatus.Pending,
                IsWithinBusinessHours = true
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (promo != null)
            {
                promo.MarkUsed(userId);
                await _context.SaveChangesAsync();
            }

            if (model.SelectedPayment == "creditcard")
            {
                order.Status = OrderStatus.PaymentConfirmed;
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    await _notificationService.SendOrderConfirmationAsync(user, order);
                }

                TempData["Success"] = $"Order #{order.OrderNumber} created and payment confirmed!";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }
            else if (model.SelectedPayment == "eft")
            {
                var eftInstruction = _paymentService.GenerateEFTInstruction(order.OrderId, order.OrderNumber, order.TotalAmount);
                TempData["EFTInstruction"] = JsonConvert.SerializeObject(eftInstruction);
                TempData["OrderId"] = order.OrderId;

                return RedirectToAction(nameof(PaymentPending), new { id = order.OrderId, method = "eft" });
            }
            else if (model.SelectedPayment == "payfast")
            {
                var user = await _context.Users.FindAsync(userId);
                var payfastPayment = _paymentService.GeneratePayFastPayment(
                    order.OrderId, order.OrderNumber, order.TotalAmount, user?.Email ?? "");

                TempData["PayFastPayment"] = JsonConvert.SerializeObject(payfastPayment);
                return Redirect(payfastPayment.PayFastUrl);
            }

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }


        [HttpGet]
        public async Task<IActionResult> PaymentPending(int id, string method)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var order = await _orderService.GetOrderAsync(id);

            if (order == null || order.UserId != userId)
                return NotFound();

            if (method == "eft")
            {
                var eftInstruction = await _paymentService.GetEFTInstruction(id);
                ViewBag.EFTInstruction = eftInstruction;
                ViewBag.PaymentMethod = "eft";
            }

            return View(order);
        }

        // ========== AUTOMATIC PRICING METHODS ==========

        private decimal GetBaseServicePrice(string serviceType)
        {
            return PricingConfig.Services.GetPrice(serviceType);
        }

        private string GetServiceDisplayName(string serviceType)
        {
            return PricingConfig.Services.GetName(serviceType);
        }

        private (string name, decimal fee) GetPackagingByTier(SubscriptionTier tier)
        {
            var tierName = tier.ToString();
            var config = PricingConfig.Packaging.Types.ContainsKey(tierName) 
                ? PricingConfig.Packaging.Types[tierName] 
                : PricingConfig.Packaging.Types["Standard"];
            return (config.Name, config.Fee);
        }

        private (string name, decimal fee) GetDeliveryByTier(SubscriptionTier tier)
        {
            var deliveryMethod = tier switch
            {
                SubscriptionTier.Standard => "Standard",
                SubscriptionTier.Premium => "Express",
                SubscriptionTier.Business => "Priority",
                _ => "Standard"
            };
            return (deliveryMethod, PricingConfig.Delivery.GetFee(deliveryMethod));
        }

        private (string name, decimal fee) GetDetergentByTier(SubscriptionTier tier)
        {
            var tierName = tier.ToString();
            var config = PricingConfig.Detergent.Types.ContainsKey(tierName) 
                ? PricingConfig.Detergent.Types[tierName] 
                : PricingConfig.Detergent.Types["Standard"];
            return (config.Name, config.Fee);
        }

        private string GetExtraDisplayName(string extraValue)
        {
            return PricingConfig.Extras.GetName(extraValue);
        }

        private decimal CalculateExtrasTotal(List<string> selectedExtras)
        {
            if (selectedExtras == null) return 0;
            return selectedExtras.Sum(extra => PricingConfig.Extras.GetPrice(extra));
        }

        private List<ServiceOption> GetAvailableServices(SubscriptionTier tier)
        {
            var availableServices = PricingConfig.Subscriptions.GetAvailableServices(tier);
            
            return availableServices.Select(service => new ServiceOption
            {
                Value = service,
                Text = PricingConfig.Services.GetName(service),
                Price = PricingConfig.Services.GetPrice(service),
                IsAvailableForPackage = true,
                PackageAvailability = tier.ToString().ToLower()
            }).ToList();
        }

        private List<ExtraOption> GetAvailableExtras()
        {
            return PricingConfig.Extras.Items.Select(extra => new ExtraOption
            {
                Value = extra.Key,
                Text = extra.Value.Name,
                Price = extra.Value.Price
            }).ToList();
        }

        private List<PaymentOption> GetPaymentMethods()
        {
            return new List<PaymentOption>
            {
                new() { Value = "creditcard", Text = "Credit Card", Icon = "fa-credit-card" },
                new() { Value = "eft", Text = "EFT / Bank Transfer", Icon = "fa-university" },
                new() { Value = "payfast", Text = "PayFast", Icon = "fa-bolt" }
            };
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }

        // ========== EXISTING ACTIONS ==========

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var order = await _orderService.GetOrderAsync(id);

            if (order == null || order.UserId != userId)
                return NotFound();

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> Track(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var order = await _orderService.GetOrderAsync(id);

            if (order == null || order.UserId != userId)
                return NotFound();

            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderBenefits(decimal subtotal, string deliveryMethod)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var subscription = await _subscriptionService.GetSubscriptionAsync(userId);
            var hasSubscription = subscription?.IsActive ?? false;
            var discountRate = subscription?.DiscountRate ?? 0;
            var discountAmount = hasSubscription ? subtotal * discountRate : 0;

            var deliveryFee = PricingConfig.Delivery.GetFee(deliveryMethod);
            var canUseFreeDelivery = await _subscriptionService.CanUseFreeDelivery(userId);
            var freeDeliveriesRemaining = await _subscriptionService.GetRemainingFreeDeliveries(userId);

            if (hasSubscription && canUseFreeDelivery && deliveryFee > 0)
            {
                deliveryFee = 0;
            }

            return Json(new
            {
                success = true,
                hasSubscription = hasSubscription,
                discountRate = discountRate,
                discountAmount = discountAmount,
                deliveryFee = deliveryFee,
                freeDeliveriesRemaining = freeDeliveriesRemaining,
                canUseFreeDelivery = canUseFreeDelivery,
                subscriptionTier = subscription?.Tier.ToString() ?? "None"
            });
        }

        private async Task NotifyAdminAboutNewOrderAsync(Order order)
        {
            OrderLogger.Instance.LogEvent($"NEW ORDER ALERT: Order #{order.OrderNumber} placed by User ID {order.UserId}");
            await Task.CompletedTask;
        }

        // ========== LEGACY METHODS (Kept for compatibility) ==========

        private decimal CalculateServiceCost(string serviceType, decimal weightKg)
        {
            var basePrice = GetBaseServicePrice(serviceType);
            var weightCost = weightKg > PricingConfig.BaseWeightKg 
                ? (weightKg - PricingConfig.BaseWeightKg) * PricingConfig.ExtraWeightRate 
                : 0;
            return basePrice + weightCost;
        }

        private decimal GetServiceRate(string serviceType)
        {
            return serviceType switch
            {
                "washonly" => 5,
                "washfold" => 7,
                "dryclean" => 12,
                "ironing" => 4,
                "express" => 15,
                _ => 5
            };
        }

        private decimal GetDeliveryFee(string deliveryMethod)
        {
            return PricingConfig.Delivery.GetFee(deliveryMethod);
        }

        private decimal GetPackagingFee(string packagingType)
        {
            return packagingType switch
            {
                "Plastic Bags" => 0,
                "Eco-Friendly" => 10,
                "Cardboard Boxes" => 15,
                _ => 0
            };
        }

        private decimal GetDetergentFee(string detergentType)
        {
            return detergentType switch
            {
                "Regular" => 0,
                "Sensitive Skin" => 15,
                "Premium" => 25,
                _ => 0
            };
        }

        private (string name, decimal fee) GetPackagingByTierLegacy(SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Standard => ("Plastic Bags", 0),
                SubscriptionTier.Premium => ("Eco-Friendly Bags", 0),
                SubscriptionTier.Business => ("Cardboard Boxes", 0),
                _ => ("Plastic Bags", 0)
            };
        }

        private (string name, decimal fee) GetDeliveryByTierLegacy(SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Standard => ("Standard (2-3 days)", PricingConfig.Delivery.Standard),
                SubscriptionTier.Premium => ("Express (Next day)", PricingConfig.Delivery.Express),
                SubscriptionTier.Business => ("Priority (Same day)", PricingConfig.Delivery.Priority),
                _ => ("Standard (2-3 days)", PricingConfig.Delivery.Standard)
            };
        }

        private (string name, decimal fee) GetDetergentByTierLegacy(SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Standard => ("Regular", 0),
                SubscriptionTier.Premium => ("Premium w/ Softener", 0),
                SubscriptionTier.Business => ("Business Grade", 0),
                _ => ("Regular", 0)
            };
        }
    }
}
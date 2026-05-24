using System.ComponentModel.DataAnnotations;

namespace Laundry.Models.ViewModels
{
    public class CreateOrderViewModel
    {
        public CreateOrderViewModel()
        {
            AvailableServices = new List<ServiceOption>();
            AvailableExtras = new List<ExtraOption>();
            PaymentMethods = new List<PaymentOption>();
            SelectedExtras = new List<string>();
            SelectedServices = new List<string>(); // Add this for multiple service selection
        }

        // Order Information
        public OrderType OrderType { get; set; } = OrderType.Delivery;

        // For backward compatibility (single service selection)
        public string? SelectedService { get; set; } = string.Empty;

        // For multiple service selection (NEW)
        public List<string> SelectedServices { get; set; }

        [Range(1, 50, ErrorMessage = "Weight must be between 1 and 50 kg")]
        public decimal WeightKg { get; set; } = 5;

        [Required(ErrorMessage = "Please select packaging type")]
        public string PackagingType { get; set; } = "Plastic Bags";

        [Required(ErrorMessage = "Please select delivery method")]
        public string DeliveryMethod { get; set; } = "Standard";

        [Required(ErrorMessage = "Please select detergent type")]
        public string DetergentType { get; set; } = "Regular";

        [Required(ErrorMessage = "Pickup address is required")]
        public string PickupAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Delivery address is required")]
        public string DeliveryAddress { get; set; } = string.Empty;

        public string? SpecialInstructions { get; set; }
        public DateTime? PreferredDate { get; set; }
        public string? PreferredTimeSlot { get; set; }

        // Payment Properties
        public string? PaymentMethod { get; set; } = "creditcard";
        public string? SelectedPayment { get; set; } = "creditcard";

        // Promo Code
        public string? PromoCode { get; set; }

        // Credit Card Properties
        public string? CardNumber { get; set; }
        public string? CardExpiry { get; set; }
        public string? CardCvv { get; set; }

        // EFT Properties
        public string? BankReference { get; set; }

        // PayFast Properties
        public string? PayFastEmail { get; set; }

        // Selected Extras
        public List<string> SelectedExtras { get; set; }

        // Available Options
        public List<ServiceOption> AvailableServices { get; set; }
        public List<ExtraOption> AvailableExtras { get; set; }
        public List<PaymentOption> PaymentMethods { get; set; }

        // Subscription properties
        public SubscriptionTier CurrentSubscriptionTier { get; set; }
        public bool HasActiveSubscription { get; set; }
        public decimal SubscriptionDiscountRate { get; set; }
        public int FreeDeliveriesRemaining { get; set; }
        public int TotalFreeDeliveries { get; set; }
        public int DeliveriesUsedThisMonth { get; set; }
        public bool FreeDeliveryApplied { get; set; }
        public string SelectedPackage { get; set; } = "Standard";

        // Price calculation properties
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ServiceOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsAvailableForPackage { get; set; }
        public string PackageAvailability { get; set; } = "all";
    }

    public class ExtraOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class PaymentOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }
}
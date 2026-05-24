namespace Laundry.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        PaymentConfirmed = 1,
        PickupAssigned = 2,
        DriverEnRoute = 3,
        DriverNearby = 4,
        DriverArrived = 5,
        PickedUp = 6,
        AtLaundry = 7,
        WashingInProgress = 8,
        Completed = 9,
        ReadyForPickup = 10,
        OutForDelivery = 11,
        Delivered = 12,
        Cancelled = 13
    }

    public enum OrderType
    {
        Pickup = 0,
        Delivery = 1
    }

    public class Order
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderType OrderType { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public decimal ServiceBasePrice { get; set; }
        public string PackageTier { get; set; } = string.Empty;
        public string PackagingType { get; set; } = string.Empty;
        public string DeliveryMethod { get; set; } = string.Empty;
        public string DetergentType { get; set; } = string.Empty;
        public string? Extras { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PromoCodeUsed { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string PickupAddress { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal WeightKg { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime? PickupDate { get; set; }
        public DateTime? DeliveryDate { get; set; }

        // ========== ADD THESE MISSING PROPERTIES ==========

        // For storing selected services as comma-separated values
        public string? SelectedServices { get; set; }

        // For storing selected extras as comma-separated values (separate from JSON Extras)
        public string? SelectedExtras { get; set; }

        // Total for all selected services
        public decimal ServicesTotal { get; set; }

        // Total for all selected extras
        public decimal ExtrasTotal { get; set; }

        // Free delivery flag
        public bool FreeDeliveryApplied { get; set; }

        // Subscription tier at time of order
        public SubscriptionTier SubscriptionTierAtOrder { get; set; }

        // Discount rate that was applied
        public decimal DiscountRateApplied { get; set; }

        // Staff Assignment
        public int? AssignedStaffId { get; set; }
        public string? AssignedStaffName { get; set; }

        // Driver Assignment
        public int? AssignedDriverId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
        public string? CurrentDriverLocation { get; set; }

        // ETA Tracking
        public int? EtaMinutes { get; set; }
        public string? DriverProximityStatus { get; set; }

        // Review and Complaint
        public bool HasComplaint { get; set; }
        public string? ComplaintReason { get; set; }
        public DateTime? ComplaintDate { get; set; }
        public string? ComplaintResolution { get; set; }
        public bool ComplaintResolved { get; set; }
        public string? CompensationPromoCode { get; set; }
        public DateTime? DriverAssignedAt { get; set; }

        // Office Hours Validation
        public bool IsWithinBusinessHours { get; set; }
        public string? BusinessHoursValidationMessage { get; set; }

        // Navigation property
        public User? User { get; set; }
    }
}
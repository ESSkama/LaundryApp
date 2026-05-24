using Laundry.Models;

namespace Laundry.Models.ViewModels
{
    public class SubscriptionUpgradeViewModel
    {
        public SubscriptionTier CurrentTier { get; set; }
        public List<SubscriptionPlan> AvailablePlans { get; set; } = new();
    }

    public class SubscriptionPlan
    {
        public SubscriptionTier Tier { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public int FreeDeliveries { get; set; }
        public List<string> AvailableServices { get; set; } = new();
        public bool IsPopular { get; set; }
        public string Features { get; set; } = string.Empty;
    }
}
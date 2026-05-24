using Laundry.Models;

namespace Laundry.Services
{
    public class SubscriptionPlanService
    {
        public List<SubscriptionPlan> GetAllPlans()
        {
            return PricingConfig.Subscriptions.Plans.Select(p => new SubscriptionPlan
            {
                Tier = p.Key,
                Name = p.Value.Name,
                MonthlyPrice = p.Value.MonthlyPrice,
                DiscountRate = p.Value.DiscountRate,
                FreeDeliveries = p.Value.FreeDeliveries,
                AvailableServices = p.Value.Services.ToList(),
                IsPopular = p.Key == SubscriptionTier.Premium,
                Features = GetFeaturesForTier(p.Key)
            }).ToList();
        }

        public SubscriptionPlan GetPlan(SubscriptionTier tier)
        {
            var config = PricingConfig.Subscriptions.GetPlan(tier);
            return new SubscriptionPlan
            {
                Tier = tier,
                Name = config.Name,
                MonthlyPrice = config.MonthlyPrice,
                DiscountRate = config.DiscountRate,
                FreeDeliveries = config.FreeDeliveries,
                AvailableServices = config.Services.ToList(),
                Features = GetFeaturesForTier(tier)
            };
        }

        private string GetFeaturesForTier(SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Standard => "5% off all orders\n2 free deliveries/month\nBasic support",
                SubscriptionTier.Premium => "10% off all orders\n5 free deliveries/month\nPriority support\nDry cleaning included",
                SubscriptionTier.Business => "15% off all orders\n10 free deliveries/month\nPriority support\nExpress service\nDedicated account manager",
                _ => string.Empty
            };
        }
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
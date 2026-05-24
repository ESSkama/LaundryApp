using Laundry.Models;

namespace Laundry.Patterns.AbstractFactory
{
    // Subscription Package Factory Interface
    public interface ISubscriptionPackageFactory
    {
        string TierName { get; }
        decimal MonthlyPrice { get; }
        decimal DiscountRate { get; }
        int FreeDeliveriesPerMonth { get; }
        List<string> AvailableServices { get; }
        string GetDescription();
    }

    // Standard Subscription Package (R49/month)
    public class StandardSubscriptionFactory : ISubscriptionPackageFactory
    {
        public string TierName => "Standard";
        public decimal MonthlyPrice => 49.00m;
        public decimal DiscountRate => 0.05m;
        public int FreeDeliveriesPerMonth => 2;

        public List<string> AvailableServices => new()
        {
            "washonly",
            "washfold"
        };

        public string GetDescription() => "Basic plan with 5% discount and 2 free deliveries per month";
    }

    // Premium Subscription Package (R99/month)
    public class PremiumSubscriptionFactory : ISubscriptionPackageFactory
    {
        public string TierName => "Premium";
        public decimal MonthlyPrice => 99.00m;
        public decimal DiscountRate => 0.10m;
        public int FreeDeliveriesPerMonth => 5;

        public List<string> AvailableServices => new()
        {
            "washonly",
            "washfold",
            "dryclean",
            "ironing"
        };

        public string GetDescription() => "Premium plan with 10% discount and 5 free deliveries per month";
    }

    // Business Subscription Package (R199/month)
    public class BusinessSubscriptionFactory : ISubscriptionPackageFactory
    {
        public string TierName => "Business";
        public decimal MonthlyPrice => 199.00m;
        public decimal DiscountRate => 0.15m;
        public int FreeDeliveriesPerMonth => 10;

        public List<string> AvailableServices => new()
        {
            "washonly",
            "washfold",
            "dryclean",
            "ironing",
            "express"
        };

        public string GetDescription() => "Business plan with 15% discount, 10 free deliveries, and priority service";
    }

    public static class SubscriptionFactoryResolver
    {
        public static ISubscriptionPackageFactory GetFactory(string tier)
        {
            return tier?.ToLower() switch
            {
                "standard" => new StandardSubscriptionFactory(),
                "premium" => new PremiumSubscriptionFactory(),
                "business" => new BusinessSubscriptionFactory(),
                _ => new StandardSubscriptionFactory()
            };
        }

        public static ISubscriptionPackageFactory GetFactoryByTier(SubscriptionTier tier)
        {
            return tier switch
            {
                SubscriptionTier.Standard => new StandardSubscriptionFactory(),
                SubscriptionTier.Premium => new PremiumSubscriptionFactory(),
                SubscriptionTier.Business => new BusinessSubscriptionFactory(),
                _ => new StandardSubscriptionFactory()
            };
        }
    }
}
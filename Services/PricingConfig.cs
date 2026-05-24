using Laundry.Models;

namespace Laundry.Services
{
    public static class PricingConfig
    {
        // Service prices (per kg or per item)
        public static class Services
        {
            public const decimal WashOnly = 150.00m;
            public const decimal WashFold = 250.00m;
            public const decimal DryClean = 450.00m;
            public const decimal Ironing = 180.00m;
            public const decimal Express = 350.00m;

            public static decimal GetPrice(string serviceType) => serviceType.ToLower() switch
            {
                "washonly" => WashOnly,      // 150
                "washfold" => WashFold,      // 250
                "dryclean" => DryClean,      // 450
                "ironing" => Ironing,        // 180
                "express" => Express,        // 350
                _ => 150.00m
            };

            public static string GetName(string serviceType) => serviceType.ToLower() switch
            {
                "washonly" => "Wash Only",
                "washfold" => "Wash & Fold",
                "dryclean" => "Dry Cleaning",
                "ironing" => "Ironing Service",
                "express" => "Express Same-Day",
                _ => serviceType
            };
        }

        // Subscription tiers
        public static class Subscriptions
        {
            public static readonly Dictionary<SubscriptionTier, SubscriptionPlanConfig> Plans = new()
            {
                {
                    SubscriptionTier.Standard, new SubscriptionPlanConfig
                    {
                        Name = "Standard",
                        MonthlyPrice = 49,
                        DiscountRate = 0.05m,
                        FreeDeliveries = 2,
                        Services = new[] { "washonly", "washfold" }
                    }
                },
                {
                    SubscriptionTier.Premium, new SubscriptionPlanConfig
                    {
                        Name = "Premium",
                        MonthlyPrice = 99,
                        DiscountRate = 0.10m,
                        FreeDeliveries = 5,
                        Services = new[] { "washonly", "washfold", "dryclean", "ironing" }
                    }
                },
                {
                    SubscriptionTier.Business, new SubscriptionPlanConfig
                    {
                        Name = "Business",
                        MonthlyPrice = 199,
                        DiscountRate = 0.15m,
                        FreeDeliveries = 10,
                        Services = new[] { "washonly", "washfold", "dryclean", "ironing", "express" }
                    }
                }
            };

            public static SubscriptionPlanConfig GetPlan(SubscriptionTier tier) => Plans[tier];
            public static decimal GetMonthlyPrice(SubscriptionTier tier) => Plans[tier].MonthlyPrice;
            public static decimal GetDiscountRate(SubscriptionTier tier) => Plans[tier].DiscountRate;
            public static int GetFreeDeliveries(SubscriptionTier tier) => Plans[tier].FreeDeliveries;
            public static string[] GetAvailableServices(SubscriptionTier tier) => Plans[tier].Services;
        }

        // Extras pricing
        public static class Extras
        {
            public static readonly Dictionary<string, ExtraConfig> Items = new()
            {
                { "fabricsoftener", new ExtraConfig { Name = "Fabric Softener", Price = 5.00m } },
                { "stainremoval", new ExtraConfig { Name = "Stain Removal", Price = 12.00m } },
                { "perfume", new ExtraConfig { Name = "Perfume Treatment", Price = 8.00m } },
                { "delicate", new ExtraConfig { Name = "Delicate Wash", Price = 10.00m } },
                { "extrafolding", new ExtraConfig { Name = "Extra Folding", Price = 7.00m } },
                { "hanger", new ExtraConfig { Name = "Hanger Packaging", Price = 9.00m } }
            };

            public static decimal GetPrice(string key) => Items.ContainsKey(key) ? Items[key].Price : 0;
            public static string GetName(string key) => Items.ContainsKey(key) ? Items[key].Name : key;
        }

        // Delivery fees
        public static class Delivery
        {
            public const decimal Standard = 50;
            public const decimal Express = 80;
            public const decimal Priority = 120;

            public static decimal GetFee(string method) => method.ToLower() switch
            {
                "express" => Express,
                "priority" => Priority,
                _ => Standard
            };
        }

        // Packaging types
        public static class Packaging
        {
            public static readonly Dictionary<string, PackagingConfig> Types = new()
            {
                { "Standard", new PackagingConfig { Name = "Plastic Bags", Fee = 0 } },
                { "Premium", new PackagingConfig { Name = "Eco-Friendly Bags", Fee = 0 } },
                { "Business", new PackagingConfig { Name = "Cardboard Boxes", Fee = 0 } }
            };
        }

        // Detergent types
        public static class Detergent
        {
            public static readonly Dictionary<string, DetergentConfig> Types = new()
            {
                { "Standard", new DetergentConfig { Name = "Regular", Fee = 0 } },
                { "Premium", new DetergentConfig { Name = "Premium w/ Softener", Fee = 0 } },
                { "Business", new DetergentConfig { Name = "Business Grade", Fee = 0 } }
            };
        }

        // Weight pricing
        public const decimal BaseWeightKg = 5;
        public const decimal ExtraWeightRate = 5;
    }

    public class SubscriptionPlanConfig
    {
        public string Name { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public int FreeDeliveries { get; set; }
        public string[] Services { get; set; } = Array.Empty<string>();
        public string[] Features { get; set; } = Array.Empty<string>();
    }

    public class ExtraConfig
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class PackagingConfig
    {
        public string Name { get; set; } = string.Empty;
        public decimal Fee { get; set; }
    }

    public class DetergentConfig
    {
        public string Name { get; set; } = string.Empty;
        public decimal Fee { get; set; }
    }
}
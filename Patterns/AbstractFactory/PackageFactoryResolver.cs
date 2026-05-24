namespace Laundry.Patterns.AbstractFactory
{
    public interface IPackageFactory
    {
        IPackaging CreatePackaging();
        IDeliveryMethod CreateDeliveryMethod();
        IDetergent CreateDetergentType();
        List<string> AvailableServices { get; }
    }

    public interface IPackaging
    {
        string Type { get; }
        decimal Cost { get; }
    }

    public interface IDeliveryMethod
    {
        string Name { get; }
        decimal Cost { get; }
    }

    public interface IDetergent
    {
        string Name { get; }
        decimal Premium { get; }
    }

    public class StandardPackageFactory : IPackageFactory
    {
        public List<string> AvailableServices => new() { "washonly", "washfold" };

        public IPackaging CreatePackaging() => new StandardPackaging();
        public IDeliveryMethod CreateDeliveryMethod() => new StandardDelivery();
        public IDetergent CreateDetergentType() => new StandardDetergent();
    }

    public class PremiumPackageFactory : IPackageFactory
    {
        public List<string> AvailableServices => new() { "washonly", "washfold", "dryclean", "ironing" };

        public IPackaging CreatePackaging() => new EcoPackaging();
        public IDeliveryMethod CreateDeliveryMethod() => new ExpressDelivery();
        public IDetergent CreateDetergentType() => new PremiumDetergent();
    }

    public class BusinessPackageFactory : IPackageFactory
    {
        public List<string> AvailableServices => new() { "washonly", "washfold", "dryclean", "ironing", "express" };

        public IPackaging CreatePackaging() => new PremiumPackaging();
        public IDeliveryMethod CreateDeliveryMethod() => new PriorityDelivery();
        public IDetergent CreateDetergentType() => new BusinessDetergent();
    }

    // Implementations
    public class StandardPackaging : IPackaging { public string Type => "Plastic Bags"; public decimal Cost => 0; }
    public class EcoPackaging : IPackaging { public string Type => "Eco-Friendly"; public decimal Cost => 10; }
    public class PremiumPackaging : IPackaging { public string Type => "Cardboard Boxes"; public decimal Cost => 15; }

    public class StandardDelivery : IDeliveryMethod { public string Name => "Standard"; public decimal Cost => 50; }
    public class ExpressDelivery : IDeliveryMethod { public string Name => "Express"; public decimal Cost => 80; }
    public class PriorityDelivery : IDeliveryMethod { public string Name => "Priority"; public decimal Cost => 120; }

    public class StandardDetergent : IDetergent { public string Name => "Regular"; public decimal Premium => 0; }
    public class PremiumDetergent : IDetergent { public string Name => "Premium"; public decimal Premium => 25; }
    public class BusinessDetergent : IDetergent { public string Name => "Business Grade"; public decimal Premium => 35; }

    public static class PackageFactoryResolver
    {
        public static IPackageFactory GetFactory(string tier)
        {
            return tier?.ToLower() switch
            {
                "standard" => new StandardPackageFactory(),
                "premium" => new PremiumPackageFactory(),
                "business" => new BusinessPackageFactory(),
                _ => new StandardPackageFactory()
            };
        }
    }
}
namespace Laundry.Patterns
{
    // Factory Method Pattern - Creates different laundry services
    public interface ILaundryService
    {
        string Name { get; }
        decimal BasePrice { get; }
        int EstimatedHours { get; }
        string Description { get; }
    }

    public class WashOnlyService : ILaundryService
    {
        public string Name => "Wash Only";
        public decimal BasePrice => 25.00m;
        public int EstimatedHours => 4;
        public string Description => "Basic washing cycle with standard detergent. No folding included.";
    }

    public class WashAndFoldService : ILaundryService
    {
        public string Name => "Wash & Fold";
        public decimal BasePrice => 35.00m;
        public int EstimatedHours => 6;
        public string Description => "Complete wash, dry, and professional folding service.";
    }

    public class DryCleaningService : ILaundryService
    {
        public string Name => "Dry Cleaning";
        public decimal BasePrice => 60.00m;
        public int EstimatedHours => 24;
        public string Description => "Professional dry cleaning for delicate and formal wear.";
    }

    public class IroningService : ILaundryService
    {
        public string Name => "Ironing Only";
        public decimal BasePrice => 20.00m;
        public int EstimatedHours => 3;
        public string Description => "Professional pressing and ironing for wrinkle-free clothes.";
    }

    public class ExpressService : ILaundryService
    {
        public string Name => "Express Same-Day";
        public decimal BasePrice => 80.00m;
        public int EstimatedHours => 2;
        public string Description => "Priority service - ready in 2 hours! Additional fee applies.";
    }

    public interface ILaundryServiceFactory
    {
        ILaundryService CreateService(string serviceType);
    }

    public class LaundryServiceFactory : ILaundryServiceFactory
    {
        public ILaundryService CreateService(string serviceType)
        {
            return serviceType.ToLower() switch
            {
                "washonly" => new WashOnlyService(),
                "washfold" => new WashAndFoldService(),
                "dryclean" => new DryCleaningService(),
                "ironing" => new IroningService(),
                "express" => new ExpressService(),
                _ => new WashAndFoldService()
            };
        }
    }
}
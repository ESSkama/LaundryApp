using Laundry.Patterns.AbstractFactory;


namespace Laundry.Models
{
    public enum SubscriptionTier
    {
        None = 0,
        Standard = 1,
        Premium = 2,
        Business = 3
    }

    public class Subscription
    {
        public int SubscriptionId { get; set; }
        public int UserId { get; set; }
        public SubscriptionTier Tier { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public int FreeDeliveriesPerMonth { get; set; }
        public int DeliveriesUsedThisMonth { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastBillingDate { get; set; }
        public DateTime? NextBillingDate { get; set; }

        // Navigation property
        public User? User { get; set; }

        public void CalculateBenefits()
        {
            var factory = SubscriptionFactoryResolver.GetFactoryByTier(Tier);
            MonthlyPrice = factory.MonthlyPrice;
            DiscountRate = factory.DiscountRate;
            FreeDeliveriesPerMonth = factory.FreeDeliveriesPerMonth;
        }

        public bool CanAccessService(string serviceType)
        {
            var factory = SubscriptionFactoryResolver.GetFactoryByTier(Tier);
            return factory.AvailableServices.Contains(serviceType);
        }

        public string GetAvailableServicesList()
        {
            var factory = SubscriptionFactoryResolver.GetFactoryByTier(Tier);
            return string.Join(", ", factory.AvailableServices);
        }

        public void ResetMonthlyCounter()
        {
            if (StartDate.Month != DateTime.Now.Month || StartDate.Year != DateTime.Now.Year)
            {
                DeliveriesUsedThisMonth = 0;
                StartDate = DateTime.Now;
            }
        }
    }
}
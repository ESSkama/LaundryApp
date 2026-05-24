namespace Laundry.Patterns
{
    // Strategy Pattern - Interchangeable payment methods
    public interface IPaymentStrategy
    {
        string Name { get; }
        string ProcessPayment(decimal amount, Dictionary<string, string> paymentDetails);
        decimal CalculateFinalAmount(decimal subtotal);
        bool ValidateDetails(Dictionary<string, string> paymentDetails);
    }

    public class CreditCardStrategy : IPaymentStrategy
    {
        public string Name => "Credit Card";

        public decimal CalculateFinalAmount(decimal subtotal) => subtotal * 1.025m; // 2.5% fee

        public bool ValidateDetails(Dictionary<string, string> details)
        {
            if (!details.ContainsKey("CardNumber")) return false;
            if (!details.ContainsKey("Expiry")) return false;
            if (!details.ContainsKey("Cvv")) return false;

            // Basic validation
            var cardNum = details["CardNumber"].Replace(" ", "");
            if (cardNum.Length < 13 || cardNum.Length > 19) return false;
            if (details["Cvv"].Length != 3 && details["Cvv"].Length != 4) return false;

            return true;
        }

        public string ProcessPayment(decimal amount, Dictionary<string, string> details)
        {
            // Simulate credit card processing
            var maskedCard = "****" + details["CardNumber"].Replace(" ", "").Substring(Math.Max(0, details["CardNumber"].Length - 4));
            return $"Payment of {amount:C} processed via Credit Card ending in {maskedCard}. Transaction ID: TX-{Guid.NewGuid():N}";
        }
    }

    public class EFTStrategy : IPaymentStrategy
    {
        public string Name => "EFT / Bank Transfer";

        public decimal CalculateFinalAmount(decimal subtotal) => subtotal * 0.95m; // 5% discount

        public bool ValidateDetails(Dictionary<string, string> details)
        {
            return details.ContainsKey("BankReference") && !string.IsNullOrEmpty(details["BankReference"]);
        }

        public string ProcessPayment(decimal amount, Dictionary<string, string> details)
        {
            return $"EFT payment initiated. Amount: {amount:C}. Reference: {details["BankReference"]}. Please allow 1-2 business days for clearance.";
        }
    }

    public class PayFastStrategy : IPaymentStrategy
    {
        public string Name => "PayFast";

        public decimal CalculateFinalAmount(decimal subtotal) => subtotal;

        public bool ValidateDetails(Dictionary<string, string> details)
        {
            return details.ContainsKey("PayFastEmail") && details["PayFastEmail"].Contains("@");
        }

        public string ProcessPayment(decimal amount, Dictionary<string, string> details)
        {
            return $"Redirecting to PayFast for payment of {amount:C}. Confirmation email sent to {details["PayFastEmail"]}.";
        }
    }

    public class SubscriptionDiscountStrategy : IPaymentStrategy
    {
        private readonly decimal _subscriptionDiscount;

        public SubscriptionDiscountStrategy(decimal subscriptionDiscount)
        {
            _subscriptionDiscount = subscriptionDiscount;
        }

        public string Name => $"Subscription Discount ({_subscriptionDiscount * 100}% off)";

        public decimal CalculateFinalAmount(decimal subtotal) => subtotal * (1 - _subscriptionDiscount);

        public bool ValidateDetails(Dictionary<string, string> details) => true;

        public string ProcessPayment(decimal amount, Dictionary<string, string> details)
        {
            return $"Subscription discount applied. Final amount: {amount:C}. Charged to your monthly subscription.";
        }
    }

    public class PromoCodeStrategy : IPaymentStrategy
    {
        private readonly string _promoCode;
        private readonly decimal _discountPercent;

        public PromoCodeStrategy(string promoCode, decimal discountPercent)
        {
            _promoCode = promoCode;
            _discountPercent = discountPercent;
        }

        public string Name => $"Promo Code: {_promoCode} ({_discountPercent}% off)";

        public decimal CalculateFinalAmount(decimal subtotal) => subtotal * (1 - _discountPercent / 100);

        public bool ValidateDetails(Dictionary<string, string> details) => true;

        public string ProcessPayment(decimal amount, Dictionary<string, string> details)
        {
            return $"Promo code '{_promoCode}' applied. You saved {_discountPercent}%. Final amount: {amount:C}.";
        }
    }

    public class LoyaltyDiscountStrategy : IPaymentStrategy
    {
        private readonly decimal _loyaltyDiscount;

        public LoyaltyDiscountStrategy(decimal loyaltyDiscount)
        {
            _loyaltyDiscount = loyaltyDiscount;
        }

        public string Name => $"Loyalty Discount ({_loyaltyDiscount * 100}% off)";

        public decimal CalculateFinalAmount(decimal subtotal) => subtotal * (1 - _loyaltyDiscount);

        public bool ValidateDetails(Dictionary<string, string> details) => true;

        public string ProcessPayment(decimal amount, Dictionary<string, string> details)
        {
            return $"Loyalty discount applied. Final amount: {amount:C}. Thank you for being a loyal customer!";
        }
    }

    public class PaymentContext
    {
        private IPaymentStrategy _strategy = null!;

        public void SetStrategy(IPaymentStrategy strategy) => _strategy = strategy;
        public decimal CalculateTotal(decimal subtotal) => _strategy.CalculateFinalAmount(subtotal);
        public string Process(decimal amount, Dictionary<string, string> details) => _strategy.ProcessPayment(amount, details);
        public bool Validate(Dictionary<string, string> details) => _strategy.ValidateDetails(details);
        public string GetStrategyName() => _strategy.Name;
    }
}
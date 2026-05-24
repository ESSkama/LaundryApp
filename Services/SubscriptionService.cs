using Laundry.Data;
using Laundry.Models;
using Laundry.Patterns.Singleton;
using Microsoft.EntityFrameworkCore;

namespace Laundry.Services
{
    public class SubscriptionService
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        // NEW: Simple method for new user subscription selection (no checking for existing)
        public async Task<(bool Success, string Message)> AddNewSubscriptionAsync(int userId, SubscriptionTier newTier)
        {
            // Check if user already has ANY subscription (active or inactive)
            var existingSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (existingSubscription != null)
            {
                // User already has a subscription - check if it's active
                if (existingSubscription.IsActive)
                {
                    // User has an active subscription - check if it's the same tier
                    if (existingSubscription.Tier == newTier)
                    {
                        return (false, $"You are already on the {newTier} plan. No changes were made.");
                    }

                    // User has a different active subscription - update it
                    return await UpdateSubscriptionAsync(userId, newTier);
                }
                else
                {
                    // User has an inactive subscription - reactivate it with new tier
                    existingSubscription.Tier = newTier;
                    existingSubscription.IsActive = true;
                    existingSubscription.StartDate = DateTime.Now;
                    existingSubscription.EndDate = null;
                    existingSubscription.DeliveriesUsedThisMonth = 0;
                    existingSubscription.CalculateBenefits();

                    await _context.SaveChangesAsync();

                    OrderLogger.Instance.LogEvent($"User {userId} reactivated {newTier} subscription");

                    _context.Notifications.Add(new Notification
                    {
                        UserId = userId,
                        Title = "Subscription Activated",
                        Message = $"Your {newTier} subscription plan is now active. Your benefits are now active!",
                        Type = "Subscription",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    return (true, $"Successfully started {newTier} subscription!");
                }
            }

            // Create brand new subscription (FIRST TIME USER - NO EXISTING SUBSCRIPTION)
            var subscription = new Subscription
            {
                UserId = userId,
                Tier = newTier,
                IsActive = true,
                StartDate = DateTime.Now,
                DeliveriesUsedThisMonth = 0
            };
            subscription.CalculateBenefits();
            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            OrderLogger.Instance.LogEvent($"User {userId} selected {newTier} subscription");

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Welcome to SUDS & SPIN!",
                Message = $"Your {newTier} subscription plan is now active. Enjoy {subscription.FreeDeliveriesPerMonth} free deliveries per month and {subscription.DiscountRate * 100}% off all services!",
                Type = "Subscription",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return (true, $"Successfully subscribed to {newTier} plan!");
        }

        // Update subscription (upgrade OR downgrade OR reactivate after cancel)
        public async Task<(bool Success, string Message)> UpdateSubscriptionAsync(int userId, SubscriptionTier newTier)
        {
            // Check for an active subscription first
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (subscription == null)
            {
                // No active subscription — check if an INACTIVE one exists (e.g. after cancellation)
                // If so, reactivate it instead of inserting a new row (avoids unique index violation)
                var existingInactive = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (existingInactive != null)
                {
                    // Reactivate the existing row with the new tier
                    existingInactive.Tier = newTier;
                    existingInactive.IsActive = true;
                    existingInactive.StartDate = DateTime.Now;
                    existingInactive.EndDate = null;
                    existingInactive.DeliveriesUsedThisMonth = 0;
                    existingInactive.CalculateBenefits();

                    await _context.SaveChangesAsync();

                    OrderLogger.Instance.LogEvent($"User {userId} reactivated {newTier} subscription");

                    _context.Notifications.Add(new Notification
                    {
                        UserId = userId,
                        Title = "Subscription Activated",
                        Message = $"Your {newTier} subscription plan is now active. Your benefits are now active!",
                        Type = "Subscription",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    return (true, $"Successfully started {newTier} subscription!");
                }

                // Truly no subscription row exists at all — safe to insert
                var newSubscription = new Subscription
                {
                    UserId = userId,
                    Tier = newTier,
                    IsActive = true,
                    StartDate = DateTime.Now,
                    DeliveriesUsedThisMonth = 0
                };
                newSubscription.CalculateBenefits();
                _context.Subscriptions.Add(newSubscription);
                await _context.SaveChangesAsync();

                OrderLogger.Instance.LogEvent($"User {userId} started new {newTier} subscription");

                _context.Notifications.Add(new Notification
                {
                    UserId = userId,
                    Title = "Subscription Started",
                    Message = $"You have started the {newTier} subscription plan. Your benefits are now active!",
                    Type = "Subscription",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return (true, $"Successfully started {newTier} subscription!");
            }

            var oldTier = subscription.Tier;

            // Same tier — nothing to do
            if (newTier == oldTier)
                return (false, $"You are already on the {oldTier} plan.");

            // Update the existing active subscription
            subscription.Tier = newTier;
            subscription.StartDate = DateTime.Now;
            subscription.DeliveriesUsedThisMonth = 0;
            subscription.IsActive = true;
            subscription.CalculateBenefits();

            await _context.SaveChangesAsync();

            var changeType = newTier > oldTier ? "upgraded" : "downgraded";
            OrderLogger.Instance.LogEvent($"User {userId} {changeType} subscription from {oldTier} to {newTier}");

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = newTier > oldTier ? "Subscription Upgraded" : "Subscription Downgraded",
                Message = $"Your subscription has been {changeType} from {oldTier} to {newTier}. Your new benefits are now active.",
                Type = "Subscription",
                IsRead = false,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return (true, $"Successfully {changeType} from {oldTier} to {newTier}!");
        }

        // Cancel subscription
        public async Task<(bool Success, string Message)> CancelSubscriptionAsync(int userId)
        {
            var subscription = await _context.Subscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (subscription == null)
                return (false, "No active subscription found.");

            subscription.IsActive = false;
            subscription.EndDate = DateTime.Now;

            await _context.SaveChangesAsync();

            OrderLogger.Instance.LogEvent($"User {userId} cancelled subscription");

            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = "Subscription Cancelled",
                Message = "Your subscription has been cancelled successfully.",
                Type = "Subscription",
                IsRead = false,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            return (true, "Subscription cancelled successfully.");
        }

        public async Task<int> GetDeliveriesUsedThisMonth(int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (subscription == null)
                return 0;

            if (subscription.StartDate.Month != DateTime.Now.Month ||
                subscription.StartDate.Year != DateTime.Now.Year)
            {
                subscription.DeliveriesUsedThisMonth = 0;
                await _context.SaveChangesAsync();
            }

            return subscription.DeliveriesUsedThisMonth;
        }

        public async Task<decimal> CalculateDiscount(int userId, decimal subtotal)
        {
            var subscription = await GetSubscriptionAsync(userId);
            if (subscription == null || !subscription.IsActive)
                return 0;

            return subtotal * subscription.DiscountRate;
        }

        public async Task<int> GetRemainingFreeDeliveries(int userId)
        {
            var subscription = await GetSubscriptionAsync(userId);
            if (subscription == null || !subscription.IsActive)
                return 0;

            var usedThisMonth = await GetDeliveriesUsedThisMonth(userId);
            return Math.Max(0, subscription.FreeDeliveriesPerMonth - usedThisMonth);
        }

        public async Task<(bool Success, string Message, decimal DiscountAmount, int FreeDeliveriesRemaining)> GetOrderBenefits(int userId, decimal subtotal)
        {
            var subscription = await GetSubscriptionAsync(userId);
            if (subscription == null || !subscription.IsActive)
                return (false, "No active subscription", 0, 0);

            var discountAmount = subtotal * subscription.DiscountRate;
            var usedDeliveries = await GetDeliveriesUsedThisMonth(userId);
            var freeDeliveriesRemaining = Math.Max(0, subscription.FreeDeliveriesPerMonth - usedDeliveries);

            return (true, $"Active {subscription.Tier} subscription", discountAmount, freeDeliveriesRemaining);
        }

        public async Task<Subscription?> GetSubscriptionAsync(int userId)
        {
            return await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
        }

        public async Task<bool> CanUseFreeDelivery(int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (subscription == null)
                return false;

            return subscription.DeliveriesUsedThisMonth < subscription.FreeDeliveriesPerMonth;
        }

        public async Task RecordDeliveryUsed(int userId)
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (subscription != null)
            {
                subscription.DeliveriesUsedThisMonth++;
                await _context.SaveChangesAsync();
            }
        }
    }
}
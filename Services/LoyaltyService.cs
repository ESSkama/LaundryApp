using Laundry.Data;
using Laundry.Models;
using Laundry.Patterns.Singleton;
using Microsoft.EntityFrameworkCore;

namespace Laundry.Services
{
    public class LoyaltyService
    {
        private readonly ApplicationDbContext _context;

        public LoyaltyService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddPointsAsync(int userId, int points)
        {
            var loyalty = await _context.LoyaltyRewards.FirstOrDefaultAsync(l => l.UserId == userId);
            if (loyalty == null)
            {
                loyalty = new LoyaltyReward
                {
                    UserId = userId,
                    Points = points,
                    TotalOrders = 1,
                    TotalSpent = 0,
                    CurrentTier = "Bronze"
                };
                _context.LoyaltyRewards.Add(loyalty);
            }
            else
            {
                loyalty.Points += points;
                loyalty.TotalOrders++;
            }

            await _context.SaveChangesAsync();
            await UpdateTierAsync(userId);

            OrderLogger.Instance.LogEvent($"User {userId} earned {points} loyalty points. Total: {loyalty.Points}");
        }

        public async Task UpdateTotalSpentAsync(int userId, decimal amount)
        {
            var loyalty = await _context.LoyaltyRewards.FirstOrDefaultAsync(l => l.UserId == userId);
            if (loyalty != null)
            {
                loyalty.TotalSpent += amount;
                await _context.SaveChangesAsync();
                await UpdateTierAsync(userId);
            }
        }

        private async Task UpdateTierAsync(int userId)
        {
            var loyalty = await _context.LoyaltyRewards.FirstOrDefaultAsync(l => l.UserId == userId);
            if (loyalty != null)
            {
                var oldTier = loyalty.CurrentTier;
                loyalty.CalculateTier();

                if (oldTier != loyalty.CurrentTier)
                {
                    OrderLogger.Instance.LogEvent($"User {userId} loyalty tier upgraded: {oldTier} → {loyalty.CurrentTier}");

                    // Send congratulation notification
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        Console.WriteLine($"[LOYALTY] Congratulations {user.FullName}! You've reached {loyalty.CurrentTier} tier!");
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<LoyaltyReward?> GetLoyaltyInfoAsync(int userId)
        {
            return await _context.LoyaltyRewards.FirstOrDefaultAsync(l => l.UserId == userId);
        }

        public async Task<decimal> GetNextTierProgressAsync(int userId)
        {
            var loyalty = await _context.LoyaltyRewards.FirstOrDefaultAsync(l => l.UserId == userId);
            if (loyalty == null) return 0;

            return loyalty.CurrentTier switch
            {
                "Bronze" => 500 - loyalty.TotalSpent,
                "Silver" => 2000 - loyalty.TotalSpent,
                "Gold" => 5000 - loyalty.TotalSpent,
                _ => 0
            };
        }
    }
}
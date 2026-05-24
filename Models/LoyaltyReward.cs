using System.ComponentModel.DataAnnotations;

namespace Laundry.Models
{
    public class LoyaltyReward
    {
        [Key]
        public int LoyaltyRewardId { get; set; }  
        public int UserId { get; set; }
        public int Points { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public string CurrentTier { get; set; } = "Bronze";
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public virtual User User { get; set; } = null!;

        public void CalculateTier()
        {
            if (TotalSpent >= 5000) CurrentTier = "Platinum";
            else if (TotalSpent >= 2000) CurrentTier = "Gold";
            else if (TotalSpent >= 500) CurrentTier = "Silver";
            else CurrentTier = "Bronze";
        }

        public decimal GetDiscountRate()
        {
            return CurrentTier switch
            {
                "Platinum" => 0.15m,
                "Gold" => 0.10m,
                "Silver" => 0.05m,
                _ => 0m
            };
        }
    }
}
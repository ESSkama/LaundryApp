using System.ComponentModel.DataAnnotations;

namespace Laundry.Models
{
    public class PromoCode
    {
        [Key]
        public int PromoId { get; set; }  

        [Required]
        public string Code { get; set; } = string.Empty;

        public decimal DiscountPercent { get; set; }

        public int MaxUses { get; set; } = 1;

        public int UsedCount { get; set; } = 0;

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        public string UsedByUserIds { get; set; } = string.Empty;

        public bool IsValidForUser(int userId)
        {
            if (!IsActive) return false;
            if (DateTime.Now > ExpiryDate) return false;
            if (UsedCount >= MaxUses) return false;

            var usedBy = UsedByUserIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (usedBy.Contains(userId.ToString())) return false;

            return true;
        }

        public void MarkUsed(int userId)
        {
            UsedCount++;
            if (string.IsNullOrEmpty(UsedByUserIds))
                UsedByUserIds = userId.ToString();
            else
                UsedByUserIds += $",{userId}";
        }
    }
}
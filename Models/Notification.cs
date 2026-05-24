namespace Laundry.Models
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // OrderConfirmation, StatusUpdate, Promo, etc.
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? OrderId { get; set; }

        // Navigation property
        public virtual User User { get; set; } = null!;
    }
}
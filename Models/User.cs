using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Laundry.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public bool IsAdmin { get; set; } = false;

        // ====== EXTENDED DISPATCH REGISTRATION PROPERTIES ======
        public string? IdentityNumber { get; set; }
        public string? SystemUsername { get; set; }
        public string? VehicleRegistrationPlate { get; set; }

        [NotMapped]
        public string DriverStatus { get; set; } = "Available";

        //role hook
        // --- CODE-ONLY DYNAMIC ROLE DISCRIMINATOR ---
        [NotMapped] // EF will completely ignore this property when writing SQL queries!
        public UserRole Role
        {
            get
            {
                if (IsAdmin) return UserRole.Admin;
                if (!string.IsNullOrEmpty(VehicleRegistrationPlate) || Email.Contains("driver", StringComparison.OrdinalIgnoreCase)) return UserRole.Driver;
                if (Email.Contains("staff", StringComparison.OrdinalIgnoreCase)) return UserRole.Staff;
                return UserRole.Customer;
            }
            set
            {
                IsAdmin = (value == UserRole.Admin);
            }
        }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        //Nav properties 
        public virtual Subscription? Subscription { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual LoyaltyReward? LoyaltyRewards { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public string GetFullAddress() => $"{Address}, {City}, {PostalCode}";
    }
}
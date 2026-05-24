using Laundry.Models;
using Microsoft.EntityFrameworkCore;
using Laundry.Services;

namespace Laundry.Data
{
    public class ApplicationDbContext : DbContext
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected ApplicationDbContext() { }

        public DbSet<User> Users { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<LoyaltyReward> LoyaltyRewards { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PaymentInstruction> PaymentInstructions { get; set; }
        public DbSet<PayFastPayment> PayFastPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure decimal precision for all decimal properties
            // LoyaltyReward
            modelBuilder.Entity<LoyaltyReward>()
                .Property(l => l.TotalSpent)
                .HasPrecision(18, 2);

            // Order decimal properties
            modelBuilder.Entity<Order>()
                .Property(o => o.ServiceBasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.Subtotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            // PromoCode
            modelBuilder.Entity<PromoCode>()
                .Property(p => p.DiscountPercent)
                .HasPrecision(18, 2);

            // Subscription
            modelBuilder.Entity<Subscription>()
                .Property(s => s.MonthlyPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Subscription>()
                .Property(s => s.DiscountRate)
                .HasPrecision(18, 2);

            // Configure primary keys
            modelBuilder.Entity<LoyaltyReward>()
                .HasKey(l => l.LoyaltyRewardId);

            modelBuilder.Entity<PromoCode>()
                .HasKey(p => p.PromoId);

            // User - Subscription (one-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Subscription)
                .WithOne(s => s.User)
                .HasForeignKey<Subscription>(s => s.UserId);

            // User - Order (one-to-many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Orders)
                .WithOne(o => o.User)
                .HasForeignKey(o => o.UserId);

            // User - LoyaltyReward (one-to-one)
            modelBuilder.Entity<User>()
                .HasOne(u => u.LoyaltyRewards)
                .WithOne(l => l.User)
                .HasForeignKey<LoyaltyReward>(l => l.UserId);

            // User - Notification (one-to-many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId);

            //seedind admin, drivers & laundry staff
            modelBuilder.Entity<User>().HasData(new User
            {
                UserId = 1,
                FullName = "System Administrator",
                Email = "admin@freshdrop.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                PhoneNumber = "0000000000",
                IsAdmin = true,
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            // Seed promo codes 
            modelBuilder.Entity<PromoCode>().HasData(
                new PromoCode
                {
                    PromoId = 1,
                    Code = "WELCOME10",
                    DiscountPercent = 10,
                    MaxUses = 100,
                    UsedCount = 0,
                    ExpiryDate = DateTime.Now.AddMonths(1),
                    IsActive = true,
                    UsedByUserIds = ""
                },
                new PromoCode
                {
                    PromoId = 2,
                    Code = "FRESH20",
                    DiscountPercent = 20,
                    MaxUses = 50,
                    UsedCount = 0,
                    ExpiryDate = DateTime.Now.AddMonths(2),
                    IsActive = true,
                    UsedByUserIds = ""
                }
            );
        } // <--- END OF ONMODELCREATING CLOSE BLOCK

        // ====== SAFE RUNTIME PROGRAMMATIC SEEDING METHOD (OUTSIDE OVERRIDES) ======
        public static void SeedDatabasePool(ApplicationDbContext context)
        {
            // 1. Double-check if our standard test driver account exists before executing insert
            if (!context.Users.Any(u => u.Email == "john.driver@freshdrop.com"))
            {
                context.Users.AddRange(
                    new User
                    {
                        FullName = "John Mkhize",
                        Email = "john.driver@freshdrop.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver@123"),
                        PhoneNumber = "0821234567",
                        IsAdmin = false,
                        IsActive = true,
                        VehicleRegistrationPlate = "EC 123-456",
                        SystemUsername = "john.m",
                        CreatedAt = DateTime.Now
                    },
                    new User
                    {
                        FullName = "Peter van der Merwe",
                        Email = "peter.driver@freshdrop.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver@123"),
                        PhoneNumber = "0837654321",
                        IsAdmin = false,
                        IsActive = true,
                        VehicleRegistrationPlate = "EC 789-012",
                        SystemUsername = "peter.v",
                        CreatedAt = DateTime.Now
                    },
                    new User
                    {
                        FullName = "Laundry Staff Mary",
                        Email = "mary.staff@freshdrop.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                        PhoneNumber = "0815556789",
                        IsAdmin = false,
                        IsActive = true,
                        SystemUsername = "mary.s",
                        CreatedAt = DateTime.Now
                    },
                    new User
                    {
                        FullName = "Laundry Staff Thabo",
                        Email = "thabo.staff@freshdrop.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                        PhoneNumber = "0849876543",
                        IsAdmin = false,
                        IsActive = true,
                        SystemUsername = "thabo.s",
                        CreatedAt = DateTime.Now
                    }
                );
                context.SaveChanges();
            }
        }
    }
}
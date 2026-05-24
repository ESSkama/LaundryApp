using Laundry.Data;
using Laundry.Models;
using Laundry.Models.ViewModels;
using Laundry.Patterns.Singleton;
using Microsoft.EntityFrameworkCore;

namespace Laundry.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterViewModel model)
        {
            // Check if email already exists
            var existingEmail = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());
            if (existingEmail != null)
            {
                return (false, "This email address is already registered. Please login instead.", null);
            }

            // Check if phone number already exists
            var existingPhone = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);
            if (existingPhone != null)
            {
                return (false, "This phone number is already registered. Please login instead.", null);
            }

            // Create new user
            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.ToLower().Trim(),
                PhoneNumber = model.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Address = model.Address,
                City = model.City,
                PostalCode = model.PostalCode,
                IsActive = true,
                IsAdmin = false,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create default subscription for user
            var subscription = new Subscription
            {
                UserId = user.UserId,
                Tier = SubscriptionTier.Standard,
                IsActive = true,
                StartDate = DateTime.Now
            };
            subscription.CalculateBenefits();
            _context.Subscriptions.Add(subscription);

            // Create loyalty rewards entry
            var loyalty = new LoyaltyReward
            {
                UserId = user.UserId,
                Points = 0,
                TotalOrders = 0,
                TotalSpent = 0,
                CurrentTier = "Bronze"
            };
            _context.LoyaltyRewards.Add(loyalty);

            await _context.SaveChangesAsync();

            OrderLogger.Instance.LogEvent($"New user registered: {user.Email}");

            return (true, "Registration successful! Please login.", user);
        }

        public async Task<(bool Success, string Message, User? User)> LoginAsync(LoginViewModel model)
        {
            // First, check if user exists
            var user = await _context.Users
                .Include(u => u.Subscription)
                .Include(u => u.LoyaltyRewards)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.ToLower());

            // Case 1: Email doesn't exist in the system
            if (user == null)
            {
                return (false, "Account not found. Please create a new account or check if you entered the correct email address.", null);
            }

            // Case 2: Account is deactivated
            if (!user.IsActive)
            {
                return (false, "Your account has been deactivated. Please contact support.", null);
            }

            // Case 3: Email exists but password is incorrect
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                // Add a small delay to prevent timing attacks
                await Task.Delay(500);
                return (false, "Invalid email or password. Please try again.", null);
            }

            // Case 4: Login successful
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            OrderLogger.Instance.LogEvent($"User logged in: {user.Email}");

            return (true, "Login successful!", user);
        }

        public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return (false, "Email is required.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                return (false, "No account found with this email address. Please check and try again.");
            }

            // Generate reset token (in production, store in database with expiry)
            var resetToken = Guid.NewGuid().ToString();

            // In a real app, save token to database
            // For demo, we'll just show it in console
            Console.WriteLine($"[PASSWORD RESET] For {email}. Token: {resetToken}");
            Console.WriteLine($"[PASSWORD RESET] Reset link: /Account/ResetPassword?email={Uri.EscapeDataString(email)}&token={resetToken}");

            OrderLogger.Instance.LogEvent($"Password reset requested for {email}");

            return (true, $"A password reset link has been sent to {email}. (Demo token: {resetToken})");
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null)
            {
                return (false, "Invalid request.");
            }

            // In real app, validate token from database
            if (string.IsNullOrEmpty(token))
            {
                return (false, "Invalid reset token.");
            }

            // Validate password strength
            if (newPassword.Length < 8)
            {
                return (false, "Password must be at least 8 characters.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            OrderLogger.Instance.LogEvent($"Password reset for {email}");

            return (true, "Password has been reset successfully. Please login with your new password.");
        }

        public async Task<bool> DeactivateAccountAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = false;

            var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
            if (subscription != null)
            {
                subscription.IsActive = false;
                subscription.EndDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            OrderLogger.Instance.LogEvent($"Account deactivated for user ID {userId}");

            return true;
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            return await _context.Users
                .Include(u => u.Subscription)
                .Include(u => u.LoyaltyRewards)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users
                .Include(u => u.Subscription)
                .Include(u => u.LoyaltyRewards)
                .FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Subscription)
                .Include(u => u.LoyaltyRewards)
                .ToListAsync();
        }

       
    }
}
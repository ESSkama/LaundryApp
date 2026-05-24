using Laundry.Data;
using Laundry.Models;
using Laundry.Models.ViewModels;
using Laundry.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Laundry.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly ApplicationDbContext _context;

        public AccountController(AuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.RegisterAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            // Store the new user ID in TempData for subscription selection
            TempData["Success"] = result.Message;
            TempData["NewUserId"] = result.User.UserId;
            TempData["NewUserEmail"] = result.User.Email;

            // Redirect to subscription selection page
            return RedirectToAction("Select", "Subscription");
        }

        [HttpPost]
        public async Task<IActionResult> CheckEmailExists(string email)
        {
            if (string.IsNullOrEmpty(email))
                return Json(new { exists = false });

            var user = await _authService.GetUserByEmail(email);
            return Json(new { exists = user != null });
        }

        [HttpPost]
        public async Task<IActionResult> CheckPhoneExists(string phone)
        {
            if (string.IsNullOrEmpty(phone))
                return Json(new { exists = false });

            var users = await _authService.GetAllUsersAsync();
            var exists = users.Any(u => u.PhoneNumber == phone);
            return Json(new { exists = exists });
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(model);

            if (!result.Success || result.User == null)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            // ====== DYNAMIC MULTI-ROLE SECURITY CLAIM INJECTION ======
            // Explicitly resolve their computed system role prior to setting context claims
            string systemRoleString = "User";
            if (result.User.IsAdmin)
            {
                systemRoleString = "Admin";
            }
            else if (result.User.Role == UserRole.Driver)
            {
                systemRoleString = "Driver";
            }
            else if (result.User.Role == UserRole.Staff)
            {
                systemRoleString = "Staff";
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.User.UserId.ToString()),
                new Claim(ClaimTypes.Email, result.User.Email),
                new Claim(ClaimTypes.Name, result.User.FullName),
                new Claim(ClaimTypes.Role, systemRoleString) // Keeps custom Authorize tags valid
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { IsPersistent = model.RememberMe });

            // ====== INTELLIGENT DISPATCH ROUTING UNIT ======
            if (result.User.IsAdmin)
            {
                return RedirectToAction("Dashboard", "Admin");
            }

            if (result.User.Role == UserRole.Driver)
            {
                return RedirectToAction("Index", "Driver"); // Drops couriers instantly into their workspace
            }

            if (result.User.Role == UserRole.Staff)
            {
                return RedirectToAction("Index", "StaffPortal"); // Placeholder destination for floor staff layout
            }

            // Customer fallback
            return RedirectToAction("Create", "Order");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Email is required.";
                return View();
            }

            var user = await _authService.GetUserByEmail(email);

            if (user == null)
            {
                TempData["Error"] = "Account not found. Please create a new account or check if you entered the correct email address.";
                return View();
            }

            var resetToken = Guid.NewGuid().ToString();

            user.PasswordResetToken = resetToken;
            user.ResetTokenExpiry = DateTime.Now.AddHours(24);
            await _context.SaveChangesAsync();

            var resetLink = Url.Action("ResetPassword", "Account",
                new { email = email, token = resetToken },
                Request.Scheme);

            Console.WriteLine($"[PASSWORD RESET] For {email}. Reset Link: {resetLink}");

            TempData["Success"] = $"A password reset link has been sent to {email}. (Demo: Click the link below)";
            TempData["ResetLink"] = resetLink;

            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Invalid password reset request.";
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == model.Email.ToLower() &&
                u.PasswordResetToken == model.Token &&
                u.ResetTokenExpiry > DateTime.Now);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid or expired reset token. Please request a new password reset.");
                return View(model);
            }

            if (BCrypt.Net.BCrypt.Verify(model.NewPassword, user.PasswordHash))
            {
                ModelState.AddModelError("", "New password cannot be the same as your current password. Please choose a different password.");
                return View(model);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpiry = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your password has been reset successfully. Please login with your new password.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Deactivate()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return RedirectToAction(nameof(Login));

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _authService.DeactivateAccountAsync(userId);
            await HttpContext.SignOutAsync();

            TempData["Success"] = "Your account has been deactivated. We're sad to see you go!";
            return RedirectToAction(nameof(Login));
        }
    }

    public class ResetPasswordViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character (@$!%*?&)")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
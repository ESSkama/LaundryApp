using Laundry.Data;
using Laundry.Models;
using Laundry.Models.ViewModels;
using Laundry.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Laundry.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly SubscriptionService _subscriptionService;
        private readonly SubscriptionPlanService _planService;
        private readonly ApplicationDbContext _context;

        public SubscriptionController(
            SubscriptionService subscriptionService,
            SubscriptionPlanService planService,
            ApplicationDbContext context)
        {
            _subscriptionService = subscriptionService;
            _planService = planService;
            _context = context;
        }

        // NEW: Subscription selection page for new users (no authorization required)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Select()
        {
            // Check if this is a new user coming from registration
            if (TempData["NewUserId"] == null && TempData["NewUserEmail"] == null)
            {
                return RedirectToAction("Register", "Account");
            }

            ViewBag.UserId = TempData["NewUserId"];
            ViewBag.UserEmail = TempData["NewUserEmail"];
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Select(int userId, string selectedTier)
        {
            if (string.IsNullOrEmpty(selectedTier))
            {
                TempData["Error"] = "Please select a subscription plan to continue.";
                TempData["NewUserId"] = userId;
                return RedirectToAction("Select");
            }

            SubscriptionTier tier = selectedTier.ToLower() switch
            {
                "premium" => SubscriptionTier.Premium,
                "business" => SubscriptionTier.Business,
                "standard" => SubscriptionTier.Standard,
                _ => SubscriptionTier.Standard
            };

            // Use AddNewSubscriptionAsync for new users
            var result = await _subscriptionService.AddNewSubscriptionAsync(userId, tier);

            if (result.Success)
            {
                // Store the selected plan for the order page
                TempData["SelectedPlan"] = selectedTier;
                TempData["Success"] = result.Message;
                return RedirectToAction("Login", "Account");
            }

            // If it failed (e.g., "already on Standard"), still allow them to continue
            // This handles the case where they might have an existing subscription
            if (result.Message.Contains("already on"))
            {
                TempData["SelectedPlan"] = selectedTier;
                TempData["Success"] = $"You are already on the {selectedTier} plan. Please login to continue.";
                return RedirectToAction("Login", "Account");
            }

            TempData["Error"] = result.Message;
            TempData["NewUserId"] = userId;
            return RedirectToAction("Select");
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var subscription = await _subscriptionService.GetSubscriptionAsync(userId);
            var usedDeliveries = await _subscriptionService.GetDeliveriesUsedThisMonth(userId);

            if (subscription == null || !subscription.IsActive)
            {
                return Json(new
                {
                    success = true,
                    hasSubscription = false,
                    tier = "None",
                    discountRate = 0,
                    freeDeliveriesRemaining = 0,
                    freeDeliveriesUsed = 0,
                    totalFreeDeliveries = 0,
                    message = "You don't have an active subscription. Upgrade to save more!"
                });
            }

            var plan = _planService.GetPlan(subscription.Tier);
            return Json(new
            {
                success = true,
                hasSubscription = true,
                tier = subscription.Tier.ToString(),
                discountRate = subscription.DiscountRate,
                freeDeliveriesRemaining = Math.Max(0, plan.FreeDeliveries - usedDeliveries),
                freeDeliveriesUsed = usedDeliveries,
                totalFreeDeliveries = plan.FreeDeliveries,
                message = $"You have {Math.Max(0, plan.FreeDeliveries - usedDeliveries)} free deliveries remaining this month",
                monthlyPrice = plan.MonthlyPrice
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderBenefits(decimal subtotal)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _subscriptionService.GetOrderBenefits(userId, subtotal);

            return Json(new
            {
                success = result.Success,
                message = result.Message,
                discountAmount = result.DiscountAmount,
                freeDeliveriesRemaining = result.FreeDeliveriesRemaining
            });
        }

        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var subscription = await _subscriptionService.GetSubscriptionAsync(userId);
            return View(subscription);
        }

        [HttpGet]
        public async Task<IActionResult> Upgrade()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Read the actual current subscription (was hardcoded to Standard before)
            var subscription = await _subscriptionService.GetSubscriptionAsync(userId);
            var currentTier = (subscription != null && subscription.IsActive)
                ? subscription.Tier
                : SubscriptionTier.None;

            // Show a message if the customer was redirected here because they have no subscription
            if (!(subscription?.IsActive ?? false))
            {
                TempData["Info"] = "You need an active subscription plan to use SUDS & SPIN. Please choose a plan below.";
            }

            var plans = _planService.GetAllPlans();

            var viewModelPlans = plans.Select(p => new Laundry.Models.ViewModels.SubscriptionPlan
            {
                Tier = p.Tier,
                Name = p.Name,
                MonthlyPrice = p.MonthlyPrice,
                DiscountRate = p.DiscountRate,
                FreeDeliveries = p.FreeDeliveries,
                AvailableServices = p.AvailableServices,
                IsPopular = p.IsPopular,
                Features = p.Features
            }).ToList();

            var model = new SubscriptionUpgradeViewModel
            {
                CurrentTier = currentTier,
                AvailablePlans = viewModelPlans
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Upgrade(SubscriptionTier newTier)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _subscriptionService.UpdateSubscriptionAsync(userId, newTier);

            if (!result.Success)
                TempData["Error"] = result.Message;
            else
                TempData["Success"] = result.Message;

            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _subscriptionService.CancelSubscriptionAsync(userId);

            if (!result.Success)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Manage));   // on failure, stay on Manage
            }

            // On success: send them to Upgrade — they cannot use the system without a plan
            TempData["Info"] = "Your subscription has been cancelled. Please select a new plan to continue using SUDS & SPIN.";
            return RedirectToAction(nameof(Upgrade));
        }
    }
}
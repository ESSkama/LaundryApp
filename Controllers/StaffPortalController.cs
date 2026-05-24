using Laundry.Data;
using Laundry.Models;
using Laundry.Patterns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Laundry.Controllers
{
    [Authorize]
    public class StaffPortalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffPortalController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Resolve the unique User ID Primary Key from the logged-in staff cookies session
            var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // 2. Load all active laundry jobs currently assigned to this employee's washing station
            var assignedWorkload = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.AssignedStaffId == staffId &&
                            o.Status != OrderStatus.Delivered &&
                            o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.Status)
                .ToListAsync();

            // 3. Compute metric targets for dashboard info summary elements
            ViewBag.ActiveWashingCount = assignedWorkload.Count(o => o.Status == OrderStatus.WashingInProgress || o.Status == OrderStatus.AtLaundry);
            ViewBag.CompletedCount = assignedWorkload.Count(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.ReadyForPickup);

            return View(assignedWorkload);
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var staff = await _context.Users.FindAsync(staffId);
            if (staff == null) return NotFound();
            return View(staff);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber, string systemUsername)
        {
            var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var staff = await _context.Users.FindAsync(staffId);
            if (staff == null) return NotFound();

            staff.FullName = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
            staff.PhoneNumber = phoneNumber;
            staff.SystemUsername = systemUsername;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Your staff profile has been updated successfully!";
            return RedirectToAction(nameof(Profile));
        }
        [HttpGet]
        public async Task<IActionResult> CompletedCycles(string? search, string? filterService, string? filterTier, int page = 1)
        {
            var staffId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            const int pageSize = 10;

           
            var query = _context.Orders
                .Include(o => o.User)
                .Where(o => o.AssignedStaffId == staffId &&
                            (o.Status == OrderStatus.Completed ||
                             o.Status == OrderStatus.ReadyForPickup ||
                             o.Status == OrderStatus.OutForDelivery ||
                             o.Status == OrderStatus.Delivered))
                .AsQueryable();

            // 2. Search by order number or customer name
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.OrderNumber.Contains(search) ||
                                         (o.User != null && o.User.FullName.Contains(search)));

            // 3. Filter by service type
            if (!string.IsNullOrWhiteSpace(filterService))
                query = query.Where(o => o.ServiceType == filterService);

            // 4. Filter by package tier
            if (!string.IsNullOrWhiteSpace(filterTier))
                query = query.Where(o => o.PackageTier == filterTier);

            // 5. Summary stats (before pagination)
            var allFiltered = await query.ToListAsync();
            ViewBag.TotalCycles = allFiltered.Count;
            ViewBag.TotalKgProcessed = allFiltered.Sum(o => o.WeightKg);
            ViewBag.TotalRevenue = allFiltered.Sum(o => o.TotalAmount);
            ViewBag.ThisWeekCount = allFiltered.Count(o => o.OrderDate >= DateTime.Today.AddDays(-7));

            // 6. Pagination metadata
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(allFiltered.Count / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.FilterService = filterService;
            ViewBag.FilterTier = filterTier;

            // 7. Dropdown options derived from this staff member's own history
            ViewBag.ServiceTypes = allFiltered.Select(o => o.ServiceType).Distinct().OrderBy(s => s).ToList();
            ViewBag.PackageTiers = allFiltered.Select(o => o.PackageTier).Distinct().OrderBy(t => t).ToList();

            var paginated = allFiltered
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(paginated);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransitionWorkState(int orderId, OrderStatus targetStatus)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();

            var oldStatus = order.Status;
            order.Status = targetStatus;

            await _context.SaveChangesAsync();

            // ====== TRIGGER OBSERVER LIFECYCLE PATTERN ======
            // Broadcasts status changes instantly across the global infrastructure alerts channel
            var statusManager = new OrderStatusManager();
            statusManager.Attach(new AdminObserver());
            statusManager.Attach(new CustomerObserver(order.User?.FullName ?? "Customer", order.User?.Email ?? "", order.User?.PhoneNumber ?? ""));

            statusManager.UpdateStatus(order.OrderId, order.OrderNumber, oldStatus, order.Status);

            TempData["Success"] = $"Order tracking updated to [{targetStatus}] successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}

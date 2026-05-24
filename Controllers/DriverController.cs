using Laundry.Data;
using Laundry.Models;
using Laundry.Patterns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Laundry.Controllers
{
    [Authorize]
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DriverController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Extract the unique database Primary Key integer assigned to this logged-in account cookie identity claim
            var driverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // 2. Look up all ongoing laundry jobs flagged under this courier profile
            var personalManifest = await _context.Orders
                .Include(o => o.User) // Eager load customer profiles to display mobile numbers & addresses
                .Where(o => o.AssignedDriverId == driverId &&
                            o.Status != OrderStatus.Delivered &&
                            o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.Status)
                .ToListAsync();

            // 3. Store summary metrics to clear clutter from core template loops
            ViewBag.ActivePickupsCount = personalManifest.Count(o => o.Status == OrderStatus.PickupAssigned || o.Status == OrderStatus.DriverEnRoute);
            ViewBag.ActiveDeliveriesCount = personalManifest.Count(o => o.Status == OrderStatus.OutForDelivery);

            return View(personalManifest);
        }

        [HttpPost]
        public async Task<IActionResult> AdvanceJobStatus(int orderId, OrderStatus nextStatus)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();

            var oldStatus = order.Status;
            order.Status = nextStatus;

            await _context.SaveChangesAsync();

            // ====== TRIGGER OBSERVER PATTERN WORKFLOW WORKLOADS ======
            // Broadcasts real-time events down to your assignment entities instantly!
            var statusManager = new OrderStatusManager();
            statusManager.Attach(new AdminObserver());
            statusManager.Attach(new CustomerObserver(
                order.User?.FullName ?? "Customer",
                order.User?.Email ?? "",
                order.User?.PhoneNumber ?? ""
            ));

            if (nextStatus == OrderStatus.PickupAssigned || nextStatus == OrderStatus.OutForDelivery)
            {
                statusManager.Attach(new DriverObserver(order.DriverName ?? "Driver", order.DriverPhone ?? ""));
            }

            statusManager.UpdateStatus(order.OrderId, order.OrderNumber, oldStatus, order.Status);

            TempData["Success"] = $"Order tracking state updated to [{nextStatus}] successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var driverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var order = await _context.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.AssignedDriverId == driverId);

            if (order == null) return NotFound();

            return View(order);
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var driverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var driver = await _context.Users.FindAsync(driverId);
            if (driver == null) return NotFound();

            return View(driver);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, string phoneNumber, string systemUsername, string vehicleRegistrationPlate)
        {
            var driverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var driver = await _context.Users.FindAsync(driverId);
            if (driver == null) return NotFound();

            // Recombine your fields safely before running your standard validations
            driver.FullName = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
            driver.PhoneNumber = phoneNumber;
            driver.SystemUsername = systemUsername;
            driver.VehicleRegistrationPlate = vehicleRegistrationPlate;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Your driver profile and transit vehicle settings have been updated successfully!";
            return RedirectToAction(nameof(Profile));
        }
        [HttpGet]
        public async Task<IActionResult> CompletedDeliveries(string? search, string? filterService, string? filterTier, int page = 1)
        {
            var driverId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            const int pageSize = 10;

            // 1. Filter orders handled by this driver that have reached final delivery states
            var query = _context.Orders
                .Include(o => o.User)
                .Where(o => o.AssignedDriverId == driverId &&
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

            // 5. Summary statistics (pre-pagination calculations)
            var allFiltered = await query.ToListAsync();
            ViewBag.TotalCycles = allFiltered.Count;
            ViewBag.TotalKgProcessed = allFiltered.Sum(o => o.WeightKg);
            ViewBag.ThisWeekCount = allFiltered.Count(o => o.OrderDate >= DateTime.Today.AddDays(-7));

            // 6. Pagination metadata & tracking state
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(allFiltered.Count / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.FilterService = filterService;
            ViewBag.FilterTier = filterTier;

            // 7. Context drop-down listings extracted from history records
            ViewBag.ServiceTypes = allFiltered.Select(o => o.ServiceType).Distinct().OrderBy(s => s).ToList();
            ViewBag.PackageTiers = allFiltered.Select(o => o.PackageTier).Distinct().OrderBy(t => t).ToList();

            var paginated = allFiltered
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(paginated);
        }



    }
}

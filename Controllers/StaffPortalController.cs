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

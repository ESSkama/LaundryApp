using Laundry.Data;
using Laundry.Models;
using Laundry.Patterns;
using Laundry.Patterns.Singleton;
using Laundry.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Laundry.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly OrderService _orderService;
        private readonly AuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly PaymentService _paymentService;

        public AdminController(
            OrderService orderService,
            AuthService authService,
            ApplicationDbContext context,
            NotificationService notificationService,
            PaymentService paymentService)
        {
            _orderService = orderService;
            _authService = authService;
            _context = context;
            _notificationService = notificationService;
            _paymentService = paymentService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var pendingOrders = await _orderService.GetPendingOrdersAsync();
            var users = await _authService.GetAllUsersAsync();

            var todayOrders = orders.Where(o => o.OrderDate.Date == DateTime.Today).Count();
            var monthlyRevenue = orders.Where(o => o.OrderDate.Month == DateTime.Now.Month && o.Status == OrderStatus.PaymentConfirmed).Sum(o => o.TotalAmount);
            var pendingEftPayments = await _context.PaymentInstructions.Where(p => !p.IsConfirmed && p.CreatedAt.AddHours(24) > DateTime.Now).CountAsync();

            // ====== PULL PERSONNEL INTO MEMORY AND FILTER SAFELY OUTSIDE OF SQL ======
            var activeUsersList = await _context.Users.Where(u => u.IsActive).ToListAsync();

            // These run in-memory and read our dynamic conditional logic flawlessly!
            ViewBag.DbDrivers = activeUsersList.Where(u => u.Role == UserRole.Driver).ToList();
            ViewBag.DbStaff = activeUsersList.Where(u => u.Role == UserRole.Staff).ToList();

            ViewBag.TotalOrders = orders.Count;
            ViewBag.PendingOrders = pendingOrders.Count;
            ViewBag.TotalCustomers = activeUsersList.Count(u => u.Role == UserRole.Customer);
            ViewBag.TotalRevenue = orders.Sum(o => o.TotalAmount);
            ViewBag.TodayOrders = todayOrders;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.PendingEftPayments = pendingEftPayments;

            return View(orders.OrderByDescending(o => o.OrderDate).Take(10).ToList());
        }
        [HttpGet]
        public async Task<IActionResult> ManageOrders(string? statusFilter, string? tierFilter, string sortOrder = "desc")
        {
            // 1. Fetch the data collection from the database, eager loading the customer profiles
            var allOrders = await _context.Orders
                .Include(o => o.User)
                .ToListAsync();

            // 2. Apply Filtering layers based on URL queries
            if (!string.IsNullOrEmpty(statusFilter))
            {
                allOrders = allOrders.Where(o => o.Status.ToString().Equals(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(tierFilter))
            {
                allOrders = allOrders.Where(o => o.PackageTier.Equals(tierFilter, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // 3. Apply Dynamic Sorting Rules (Default is Newest to Oldest)
            if (sortOrder == "asc")
            {
                allOrders = allOrders.OrderBy(o => o.OrderDate).ToList();
            }
            else
            {
                allOrders = allOrders.OrderByDescending(o => o.OrderDate).ToList();
            }

            // 4. Pass the current configuration down to the view so the dropdowns remember what was selected
            ViewBag.CurrentStatusFilter = statusFilter;
            ViewBag.CurrentTierFilter = tierFilter;
            ViewBag.CurrentSortOrder = sortOrder;

            // Load list options dynamically for the filter dropdown elements
            ViewBag.Statuses = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>().ToList();
            ViewBag.Tiers = new List<string> { "Standard", "Premium", "Business" };

            return View(allOrders);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            // 1. Fetch the order using your existing service layer
            var order = await _orderService.GetOrderAsync(id);
            if (order == null)
                return NotFound();

            // 2. Explicitly load the User profile from the database if it wasn't eager-loaded
            if (order.User == null)
            {
                order.User = await _context.Users.FirstOrDefaultAsync(u => u.UserId == order.UserId);
            }

            // Load dynamic dropdown selections from memory array matching our code-only enum definitions
            var activeUsersList = await _context.Users.Where(u => u.IsActive).ToListAsync();
            ViewBag.DbDrivers = activeUsersList.Where(u => u.Role == UserRole.Driver).ToList();
            ViewBag.DbStaff = activeUsersList.Where(u => u.Role == UserRole.Staff).ToList();

            // 3. Get EFT instruction if it exists
            var eftInstruction = await _paymentService.GetEFTInstruction(id);
            ViewBag.EFTInstruction = eftInstruction;

            // 4. Return the fully populated order object safely to your new template layout
            return View(order);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateOrderStatus(int id)
        {
            var order = await _orderService.GetOrderAsync(id);
            if (order == null)
                return NotFound();

            ViewBag.Statuses = Enum.GetValues(typeof(OrderStatus))
                .Cast<OrderStatus>()
                .Select(s => new { Value = (int)s, Name = s.ToString() })
                .ToList();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus newStatus, string? staffName = null)
        {
            var adminId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _orderService.UpdateOrderStatusAsync(id, newStatus, adminId, staffName);

            TempData["Success"] = $"Order #{id} status updated to {newStatus}";
            return RedirectToAction(nameof(ManageOrders));
        }

        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            // 1. Pull the data structure into memory first, eagerly loading sub-tables
            var allActivePersonnel = await _context.Users
                .Include(u => u.Subscription)
                .Include(u => u.Orders)
                .ToListAsync();

            // 2. Perform the role enum filtering safely on the client side (in-memory)
            var customers = allActivePersonnel
                .Where(u => !u.IsAdmin && u.Role == UserRole.Customer)
                .OrderByDescending(u => u.CreatedAt)
                .ToList();

            return View(customers);
        }

        [HttpGet]
        public async Task<IActionResult> UserHistory(int id)
        {
            // 1. Fetch the targeted customer account profile
            var customer = await _context.Users.FindAsync(id);
            if (customer == null) return NotFound();

            // 2. Compile their complete historical lifecycle ledger
            var orderHistory = await _context.Orders
                .Where(o => o.UserId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // 3. Compute dynamic historical metrics safely for display
            ViewBag.CustomerName = customer.FullName;
            ViewBag.CustomerEmail = customer.Email;
            ViewBag.CustomerPhone = customer.PhoneNumber;
            ViewBag.TotalOrdersCount = orderHistory.Count;
            ViewBag.LifetimeSpend = orderHistory.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount);

            return View(orderHistory);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int userId, bool isActive)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsActive = isActive;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"User {user.Email} {(isActive ? "activated" : "deactivated")}";
            }
            return RedirectToAction(nameof(ManageUsers));
        }


        [HttpGet]
        public async Task<IActionResult> Drivers()
        {
            var allUsers = await _context.Users.Where(u => u.IsActive).ToListAsync();
            var driversInDb = allUsers.Where(u => u.Role == UserRole.Driver).ToList();

            var activeOrders = await _context.Orders
                .Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                .ToListAsync();

            var viewModel = driversInDb.Select(d => {
                int orderCount = activeOrders.Count(o => o.AssignedDriverId == d.UserId);

                // If they have ongoing collection or delivery packages, show them on route
                string computedStatus = "Available";
                if (orderCount > 0)
                {
                    computedStatus = "On Delivery";
                }

                return new DriverInfo
                {
                    Id = d.UserId,
                    Name = d.FullName,
                    Phone = d.PhoneNumber,
                    Status = computedStatus,
                    CurrentOrders = orderCount
                };
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDriver(string fullName, string phone, string email, string identityNumber, string username, string vehiclePlate)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                TempData["Error"] = "An account with this email already exists.";
                return RedirectToAction(nameof(Drivers));
            }

            var newDriver = new User
            {
                FullName = fullName,
                PhoneNumber = phone,
                Email = email,
                IdentityNumber = identityNumber,
                SystemUsername = username,
                VehicleRegistrationPlate = vehiclePlate,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Driver@123"),
                IsAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newDriver);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Driver {fullName} registered successfully with vehicle plate [{vehiclePlate}]!";
            return RedirectToAction(nameof(Drivers));
        }

        // ====== DYNAMIC CARE WORKER ASSEMBLY VIEW LAYER (STAFF) ======
        [HttpGet]
        public async Task<IActionResult> Staff()
        {
            var allUsers = await _context.Users.Where(u => u.IsActive).ToListAsync();
            var staffInDb = allUsers.Where(u => u.Role == UserRole.Staff).ToList();

            // Track laundry loads assigned to them that are active on the production floor
            var activeOrders = await _context.Orders
                .Where(o => o.Status == OrderStatus.PickupAssigned ||
                            o.Status == OrderStatus.DriverEnRoute ||
                            o.Status == OrderStatus.PickedUp ||
                            o.Status == OrderStatus.AtLaundry ||
                            o.Status == OrderStatus.WashingInProgress)
                .ToListAsync();

            var viewModel = staffInDb.Select(s => {
                int loadCount = activeOrders.Count(o => o.AssignedStaffId == s.UserId);

                return new StaffInfo
                {
                    Id = s.UserId,
                    Name = s.FullName,
                    Role = "Plant Operator",
                    Status = loadCount > 0 ? $"Processing ({loadCount} Loads)" : "Ready/Available"
                };
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> CreateStaff(string fullName, string phone, string email, string identityNumber, string username)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                TempData["Error"] = "An account with this email already exists.";
                return RedirectToAction(nameof(Staff));
            }

            var newStaff = new User
            {
                FullName = fullName,
                PhoneNumber = phone,
                Email = email,
                IdentityNumber = identityNumber,
                SystemUsername = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123"),
                IsAdmin = false,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newStaff);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Production operator {fullName} added successfully to the system registry!";
            return RedirectToAction(nameof(Staff));
        }

        [HttpGet]
        public async Task<IActionResult> Subscriptions()
        {
            var subscriptions = await _context.Subscriptions
                .Include(s => s.User)
                .Where(s => s.IsActive)
                .ToListAsync();
            return View(subscriptions);
        }

        [HttpGet]
        public async Task<IActionResult> PendingPayments()
        {
            var pendingInstructions = await _context.PaymentInstructions
                .Where(p => !p.IsConfirmed)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(pendingInstructions);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmEftPayment(int instructionId)
        {
            var instruction = await _context.PaymentInstructions.FindAsync(instructionId);
            if (instruction != null)
            {
                instruction.IsConfirmed = true;
                instruction.ConfirmedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Update order status
                var order = await _context.Orders.FindAsync(instruction.OrderId);
                if (order != null && order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.PaymentConfirmed;
                    await _context.SaveChangesAsync();

                    // Notify customer
                    var user = await _context.Users.FindAsync(order.UserId);
                    if (user != null)
                    {
                        await _notificationService.SendOrderConfirmationAsync(user, order);
                    }
                }

                TempData["Success"] = $"EFT payment for order #{instruction.OrderNumber} confirmed";
            }
            return RedirectToAction(nameof(PendingPayments));
        }

        [HttpGet]
        public async Task<IActionResult> Reports()
        {
            var orders = await _orderService.GetAllOrdersAsync();

            // Monthly revenue report
            var monthlyRevenue = orders
                .Where(o => o.Status == OrderStatus.PaymentConfirmed || o.Status == OrderStatus.Delivered)
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlyReport
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.Year).ThenByDescending(r => r.Month)
                .ToList();

            // Service popularity
            var servicePopularity = orders
                .GroupBy(o => o.ServiceType)
                .Select(g => new ServiceReport
                {
                    ServiceName = g.Key,
                    Count = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.ServicePopularity = servicePopularity;

            return View();
        }

        [HttpGet]
        public IActionResult Logs()
        {
            var logs = OrderLogger.Instance.GetLogEntries();
            var config = AppConfigManager.Instance.GetAllSettings();

            ViewBag.Config = config;
            return View(logs);
        }

        [HttpPost]
        public IActionResult ClearLogs()
        {
            OrderLogger.Instance.ClearLogs();
            TempData["Success"] = "Logs cleared successfully";
            return RedirectToAction(nameof(Logs));
        }

        [HttpGet]
        public IActionResult LogStatistics()
        {
            var stats = OrderLogger.Instance.GetLogStatistics();
            return Json(stats);
        }



        //assignment
        [HttpPost]
        public async Task<IActionResult> AssignLogisticsAndProduction(int orderId, int driverId, int staffId)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();

            var driverUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == driverId);
            var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == staffId);

            if (driverUser != null && staffUser != null)
            {
                var oldStatus = order.Status;

                // Map allocation properties safely
                order.AssignedDriverId = driverUser.UserId;
                order.DriverName = driverUser.FullName;
                order.DriverPhone = driverUser.PhoneNumber;
                order.DriverAssignedAt = DateTime.Now;

                order.AssignedStaffId = staffUser.UserId;
                order.AssignedStaffName = staffUser.FullName;

                // Advance baseline lifecycle status
                order.Status = OrderStatus.PickupAssigned;

                await _context.SaveChangesAsync();

                // Trigger the Observer Pattern Workflow
                var statusManager = new OrderStatusManager();
                statusManager.Attach(new AdminObserver());
                statusManager.Attach(new CustomerObserver(order.User?.FullName ?? "Customer", order.User?.Email ?? "", order.User?.PhoneNumber ?? ""));
                statusManager.Attach(new DriverObserver(driverUser.FullName, driverUser.PhoneNumber));
                statusManager.Attach(new LaundryStaffObserver(staffUser.FullName));

                statusManager.UpdateStatus(order.OrderId, order.OrderNumber, oldStatus, order.Status);

                TempData["Success"] = "Logistics driver and production team member allocated simultaneously!";
            }

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        [HttpPost]
        public async Task<IActionResult> DispatchForFinalDelivery(int orderId, int deliveryDriverId)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();

            var driverUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == deliveryDriverId);
            if (driverUser != null)
            {
                var oldStatus = order.Status;

                // Re-assign or confirm delivery driver info
                order.AssignedDriverId = driverUser.UserId;
                order.DriverName = driverUser.FullName;
                order.DriverPhone = driverUser.PhoneNumber;
                order.Status = OrderStatus.OutForDelivery;

                await _context.SaveChangesAsync();

                // Broadcast notification updates using Observers
                var statusManager = new OrderStatusManager();
                statusManager.Attach(new AdminObserver());
                statusManager.Attach(new CustomerObserver(order.User?.FullName ?? "Customer", order.User?.Email ?? "", order.User?.PhoneNumber ?? ""));
                statusManager.Attach(new DriverObserver(driverUser.FullName, driverUser.PhoneNumber));

                statusManager.UpdateStatus(order.OrderId, order.OrderNumber, oldStatus, order.Status);

                TempData["Success"] = $"Order #{order.OrderNumber} is out for final delivery with {driverUser.FullName}!";
            }

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

        // SIMULATION: This endpoint mimics the staff member clicking "Mark Done" in their own dashboard portal snippet
        [HttpPost]
        public async Task<IActionResult> SimulateStaffCompletion(int orderId)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) return NotFound();

            var oldStatus = order.Status;
            order.Status = OrderStatus.Completed; // Matches the targeted completed check flag state

            await _context.SaveChangesAsync();

            var statusManager = new OrderStatusManager();
            statusManager.Attach(new AdminObserver());
            statusManager.Attach(new CustomerObserver(order.User?.FullName ?? "Customer", order.User?.Email ?? "", order.User?.PhoneNumber ?? ""));

            statusManager.UpdateStatus(order.OrderId, order.OrderNumber, oldStatus, order.Status);

            TempData["Success"] = $"Staff task marked completed. Order is ready for final delivery dispatch!";
            return RedirectToAction("OrderDetails", new { id = orderId });
        }
    }



    public class DriverInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int CurrentOrders { get; set; }
    }

    public class StaffInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class MonthlyReport
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

    public class ServiceReport
    {
        public string ServiceName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Revenue { get; set; }
    }
}
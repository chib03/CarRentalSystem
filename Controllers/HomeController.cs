using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // ADMIN DASHBOARD
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var completedAmounts = _context.RentalTransactions
                    .Where(r => r.Status == "Completed")
                    .AsEnumerable()
                    .Sum(r => r.TotalAmount);

                var dash = new DashboardViewModel
                {
                    TotalCars = await _context.Cars.CountAsync(),
                    AvailableCars = await _context.Cars.CountAsync(c => c.Status == "Available"),
                    RentedCars = await _context.Cars.CountAsync(c => c.Status == "Rented"),
                    TotalCustomers = await _context.Customers.CountAsync(),
                    TotalRentals = await _context.RentalTransactions.CountAsync(),
                    ActiveRentals = await _context.RentalTransactions.CountAsync(r => r.Status == "Active"),
                    TotalRevenue = completedAmounts,
                    RecentRentals = await _context.RentalTransactions
                                        .Include(r => r.Customer)
                                        .Include(r => r.Car)
                                        .OrderByDescending(r => r.CreatedAt)
                                        .Take(5)
                                        .ToListAsync()
                };

                return View("AdminDashboard", dash);
            }

            // CUSTOMER DASHBOARD
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.UserId == user.Id);

            ViewBag.Customer = customer;

            ViewBag.MyRentals = customer != null
                ? await _context.RentalTransactions
                    .Include(r => r.Car)
                    .Where(r => r.CustomerId == customer.Id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync()
                : new List<RentalTransaction>();

            var cars = await _context.Cars
                .Where(c => c.Status == "Available")
                .ToListAsync();

            return View("CustomerDashboard", cars);
        }
    }
}
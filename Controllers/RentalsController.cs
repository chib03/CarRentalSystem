using CarRentalSystem.Data;
using CarRentalSystem.Models;
using CarRentalSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{
    [Authorize]
    public class RentalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;

        public RentalsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, EmailService emailService)
        { _context = context; _userManager = userManager; _emailService = emailService;}

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string? status, string? search)
        {
            var q = _context.RentalTransactions.Include(r => r.Customer).Include(r => r.Car).AsQueryable();
            if (!string.IsNullOrEmpty(status)) q = q.Where(r => r.Status == status);
            if (!string.IsNullOrEmpty(search)) q = q.Where(r => r.Customer!.FullName.Contains(search) || r.Car!.Brand.Contains(search) || r.Car!.Model.Contains(search));
            ViewBag.Status = status; ViewBag.Search = search;
            return View(await q.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            return View(new RentalCreateViewModel
            {
                Customers = await _context.Customers.OrderBy(c => c.FullName).ToListAsync(),
                AvailableCars = await _context.Cars.Where(c => c.Status == "Available").OrderBy(c => c.Brand).ToListAsync()
            });
        }

        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RentalCreateViewModel model)
        {
            if (model.ReturnDate <= model.RentalDate) ModelState.AddModelError("ReturnDate", "Return date must be after rental date.");
            var car = await _context.Cars.FindAsync(model.CarId);
            if (car == null || car.Status == "Rented") ModelState.AddModelError("CarId", "Car not available.");

            if (ModelState.IsValid && car != null)
            {
                var days = (model.ReturnDate - model.RentalDate).Days;
                car.Status = "Rented";
                _context.RentalTransactions.Add(new RentalTransaction
                {
                    CustomerId = model.CustomerId,
                    CarId = model.CarId,
                    RentalDate = model.RentalDate,
                    ReturnDate = model.ReturnDate,
                    TotalAmount = car.DailyRate * days,
                    Status = "Active",
                    Notes = model.Notes
                });
                await _context.SaveChangesAsync(); TempData["Success"] = "Rental created!";
                return RedirectToAction(nameof(Index));
            }
            model.Customers = await _context.Customers.OrderBy(c => c.FullName).ToListAsync();
            model.AvailableCars = await _context.Cars.Where(c => c.Status == "Available").ToListAsync();
            return View(model);
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> BookRequest(int carId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null)
            {
                customer = new Customer { FullName = user.FullName, Email = user.Email ?? "", Phone = "", Address = "", LicenseNumber = "", UserId = user.Id };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var car = await _context.Cars.FindAsync(carId);
            if (car == null || car.Status != "Available") { TempData["Error"] = "This car is no longer available."; return RedirectToAction("Index", "Home"); }

            return View(new RentalCreateViewModel
            {
                CarId = carId,
                CustomerId = customer.Id,
                RentalDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(1),
                AvailableCars = new List<Car> { car },
                Customers = new List<Customer> { customer }
            });
        }

        [HttpPost, Authorize(Roles = "Customer"), ValidateAntiForgeryToken]
        public async Task<IActionResult> BookRequest(RentalCreateViewModel model)
        {
            if (model.ReturnDate <= model.RentalDate) ModelState.AddModelError("ReturnDate", "Return date must be after rental date.");
            var car = await _context.Cars.FindAsync(model.CarId);
            if (car == null || car.Status == "Rented") ModelState.AddModelError("CarId", "Car no longer available.");

            if (ModelState.IsValid && car != null)
            {
                var days = Math.Max(1, (model.ReturnDate - model.RentalDate).Days);
                _context.RentalTransactions.Add(new RentalTransaction
                {
                    CustomerId = model.CustomerId,
                    CarId = model.CarId,
                    RentalDate = model.RentalDate,
                    ReturnDate = model.ReturnDate,
                    TotalAmount = car.DailyRate * days,
                    Status = "Pending",
                    Notes = model.Notes
                });
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Booking submitted! Admin will review and confirm soon.";
                return RedirectToAction("Index", "Home");
            }

            if (car != null) model.AvailableCars = new List<Car> { car };
            var user = await _userManager.GetUserAsync(User);
            var cust = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user!.Id);
            model.Customers = cust != null ? new List<Customer> { cust } : new();
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var r = await _context.RentalTransactions.Include(x => x.Customer).Include(x => x.Car).FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return NotFound(); return View(r);
        }
        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string status, string? notes, DateTime? actualReturnDate)
        {
            var r = await _context.RentalTransactions
                .Include(x => x.Car)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null) return NotFound();

            var oldStatus = r.Status;
            r.Status = status;
            r.Notes = notes;

            if (r.Customer != null && !string.IsNullOrEmpty(r.Customer.Email))
            {
                // --- ORIGINAL MESSAGE MO PARA SA ACTIVE ---
                if (status == "Active" && oldStatus == "Pending" && r.Car != null)
                {
                    r.Car.Status = "Rented";
                    string subject = "🚗 Ready for Pick-up: Your Car Rental Booking";
                    string body = $@"
                <div style='font-family: sans-serif; line-height: 1.6; color: #333;'>
                    <h2 style='color: #28a745;'>Booking Accepted!</h2>
                    <p>Hello <strong>{r.Customer.FullName}</strong>,</p>
                    <p>Good news! Your request to rent the <strong>{r.Car.Brand} {r.Car.Model}</strong> has been approved.</p>
                    <p style='background-color: #f8f9fa; padding: 15px; border-left: 5px solid #28a745;'>
                        <strong>Status:</strong> Ready to pick up at our garage.<br>
                        <strong>Rental Date:</strong> {r.RentalDate.ToShortDateString()}<br>
                        <strong>Return Date:</strong> {r.ReturnDate.ToShortDateString()}
                    </p>
                    <p>Please bring your <strong>Driver's License</strong> and the <strong>Total Amount: ₱{r.TotalAmount:N2}</strong>.</p>
                    <p>See you at the garage!</p>
                    <hr>
                    <small>This is an automated message from DriveEase Rental System.</small>
                </div>";

                    await _emailService.SendEmailAsync(r.Customer.Email, subject, body);
                }
                // --- DAGDAG NA LOGIC PARA SA CANCELLED (REJECT) ---
                // Ginawa nating status == "Cancelled" para mag-match sa View mo
                else if (status == "Cancelled" && oldStatus == "Pending")
                {
                    if (r.Car != null) r.Car.Status = "Available";

                    string subject = "❌ Update: Your Car Rental Booking Request";
                    string body = $@"
                <div style='font-family: sans-serif; line-height: 1.6; color: #333;'>
                    <h2 style='color: #dc3545;'>Booking Request Update</h2>
                    <p>Hello <strong>{r.Customer.FullName}</strong>,</p>
                    <p>We regret to inform you that your booking request for the <strong>{r.Car?.Brand} {r.Car?.Model}</strong> has been <strong>Rejected/Cancelled</strong>.</p>
                    <p><strong>Reason/Notes:</strong> {(string.IsNullOrEmpty(notes) ? "No additional notes provided." : notes)}</p>
                    <p>Feel free to browse our other available cars for your next trip.</p>
                    <hr>
                    <small>DriveEase Rental System</small>
                </div>";

                    await _emailService.SendEmailAsync(r.Customer.Email, subject, body);
                }
            }

            if ((status == "Completed" || status == "Cancelled") && r.Car != null)
            {
                r.Car.Status = "Available";
                if (status == "Completed") r.ActualReturnDate = actualReturnDate ?? DateTime.Today;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Rental updated and notification sent!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var r = await _context.RentalTransactions.Include(x => x.Customer).Include(x => x.Car).FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return NotFound(); return View(r);
        }

        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _context.RentalTransactions.Include(x => x.Car).FirstOrDefaultAsync(x => x.Id == id);
            if (r == null) return NotFound();
            if (r.Status == "Active") { TempData["Error"] = "Cannot delete an active rental."; return RedirectToAction(nameof(Index)); }
            if (r.Car != null && r.Status != "Completed") r.Car.Status = "Available";
            _context.RentalTransactions.Remove(r); await _context.SaveChangesAsync(); TempData["Success"] = "Rental deleted!";
            return RedirectToAction(nameof(Index));
        }
    }
}
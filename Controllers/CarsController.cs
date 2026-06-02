using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{
    [Authorize]
    public class CarsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CarsController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var q = _context.Cars.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(c => c.Brand.Contains(search) || c.Model.Contains(search) || c.PlateNumber.Contains(search));
            if (!string.IsNullOrEmpty(status)) q = q.Where(c => c.Status == status);
            ViewBag.Search = search; ViewBag.Status = status;
            return View(await q.OrderBy(c => c.Brand).ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car)
        {
            if (ModelState.IsValid) { _context.Cars.Add(car); await _context.SaveChangesAsync(); TempData["Success"] = "Car added!"; return RedirectToAction(nameof(Index)); }
            return View(car);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        { var car = await _context.Cars.FindAsync(id); if (car == null) return NotFound(); return View(car); }

        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Car car)
        {
            if (id != car.Id) return NotFound();
            if (ModelState.IsValid) { _context.Update(car); await _context.SaveChangesAsync(); TempData["Success"] = "Car updated!"; return RedirectToAction(nameof(Index)); }
            return View(car);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var car = await _context.Cars.Include(c => c.Rentals).ThenInclude(r => r.Customer).FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound(); return View(car);
        }

        [HttpPost, Authorize(Roles = "Admin"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var car = await _context.Cars.FindAsync(id); if (car == null) return NotFound();
            if (car.Status == "Rented") { TempData["Error"] = "Cannot delete a rented car."; return RedirectToAction(nameof(Index)); }
            _context.Cars.Remove(car); await _context.SaveChangesAsync(); TempData["Success"] = "Car deleted!";
            return RedirectToAction(nameof(Index));
        }
    }
}
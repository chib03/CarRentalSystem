using CarRentalSystem.Data;
using CarRentalSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CustomersController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index(string? search)
        {
            var q = _context.Customers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                q = q.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || c.Phone.Contains(search));
            ViewBag.Search = search;
            return View(await q.OrderBy(c => c.FullName).ToListAsync());
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer c)
        {
            if (ModelState.IsValid) { _context.Customers.Add(c); await _context.SaveChangesAsync(); TempData["Success"] = "Customer added!"; return RedirectToAction(nameof(Index)); }
            return View(c);
        }

        public async Task<IActionResult> Edit(int id)
        { var c = await _context.Customers.FindAsync(id); if (c == null) return NotFound(); return View(c); }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer c)
        {
            if (id != c.Id) return NotFound();
            if (ModelState.IsValid) { _context.Update(c); await _context.SaveChangesAsync(); TempData["Success"] = "Customer updated!"; return RedirectToAction(nameof(Index)); }
            return View(c);
        }

        public async Task<IActionResult> Details(int id)
        {
            var c = await _context.Customers.Include(x => x.Rentals).ThenInclude(r => r.Car).FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound(); return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _context.Customers.Include(x => x.Rentals).FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();
            if (c.Rentals.Any(r => r.Status == "Active")) { TempData["Error"] = "Cannot delete customer with active rentals."; return RedirectToAction(nameof(Index)); }
            _context.Customers.Remove(c); await _context.SaveChangesAsync(); TempData["Success"] = "Customer deleted!";
            return RedirectToAction(nameof(Index));
        }
    }
}
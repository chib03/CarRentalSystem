using CarRentalSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem.Controllers
{ 
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UsersController(UserManager<ApplicationUser> userManager) { _userManager = userManager; }

        public async Task<IActionResult> Index()
            => View(await _userManager.Users.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync());

        [HttpGet] public IActionResult Create() => View(new AdminUserViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (await _userManager.FindByNameAsync(model.Username) != null) { ModelState.AddModelError("Username", "Username taken."); return View(model); }

            var user = new ApplicationUser { UserName = model.Username, Email = model.Email, FullName = model.FullName, Role = model.Role, EmailConfirmed = true, IsEmailVerified = true };
            var r = await _userManager.CreateAsync(user, model.Password);
            if (r.Succeeded) { await _userManager.AddToRoleAsync(user, model.Role); TempData["Success"] = "User created!"; return RedirectToAction(nameof(Index)); }
            foreach (var e in r.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id); if (user == null) return NotFound();
            ViewBag.UserId = id;
            return View(new AdminUserViewModel { FullName = user.FullName, Username = user.UserName ?? "", Email = user.Email ?? "", Role = user.Role, Password = "placeholder", ConfirmPassword = "placeholder" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AdminUserViewModel model, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(id); if (user == null) return NotFound();
            user.FullName = model.FullName; user.Email = model.Email;
            if (user.Role != model.Role) { await _userManager.RemoveFromRoleAsync(user, user.Role); user.Role = model.Role; await _userManager.AddToRoleAsync(user, model.Role); }
            if (!string.IsNullOrWhiteSpace(newPassword) && newPassword.Length >= 6) { var t = await _userManager.GeneratePasswordResetTokenAsync(user); await _userManager.ResetPasswordAsync(user, t, newPassword); }
            await _userManager.UpdateAsync(user); TempData["Success"] = "User updated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me?.Id == id) { TempData["Error"] = "Cannot delete your own account."; return RedirectToAction(nameof(Index)); }
            var user = await _userManager.FindByIdAsync(id); if (user == null) return NotFound();
            if (user.UserName == "admin") { TempData["Error"] = "Cannot delete the main admin."; return RedirectToAction(nameof(Index)); }
            await _userManager.DeleteAsync(user); TempData["Success"] = "User deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
  
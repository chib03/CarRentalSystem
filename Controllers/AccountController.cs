using CarRentalSystem.Data;
using CarRentalSystem.Models;
using CarRentalSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            EmailService emailService, ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);


            var user = await _userManager.FindByNameAsync(model.Username)
                    ?? await _userManager.FindByEmailAsync(model.Username);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

            if (user.Role == "Customer" && !user.IsEmailVerified)
            {
                HttpContext.Session.SetString("VerifyUserId", user.Id);
                HttpContext.Session.SetString("VerifyEmail", user.Email ?? "");
                TempData["Error"] = "Please verify your email first.";
                return RedirectToAction("VerifyOtp");
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
            if (!result.Succeeded) { ModelState.AddModelError("", "Invalid username or password."); return View(model); }

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _userManager.FindByNameAsync(model.Username) != null)
            { ModelState.AddModelError("Username", "Username is already taken."); return View(model); }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            { ModelState.AddModelError("Email", "An account with this email already exists."); return View(model); }

            var otp = new Random().Next(100000, 999999).ToString();

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                Role = "Customer",
                EmailConfirmed = false,
                IsEmailVerified = false,
                EmailOtp = otp,
                EmailOtpExpiry = DateTime.Now.AddMinutes(10)
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            { foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); return View(model); }

            await _userManager.AddToRoleAsync(user, "Customer");

            _context.Customers.Add(new Customer
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = "",
                Address = "",
                LicenseNumber = "",
                UserId = user.Id
            });
            await _context.SaveChangesAsync();

            var sent = await _emailService.SendOtpEmailAsync(model.Email, model.FullName, otp);

            HttpContext.Session.SetString("VerifyUserId", user.Id);
            HttpContext.Session.SetString("VerifyEmail", user.Email ?? "");

            TempData["Success"] = sent
                ? $"We sent a 6-digit code to {model.Email}. Enter it below."
                : $"Account created! Email failed — dev OTP: {otp}";

            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var userId = HttpContext.Session.GetString("VerifyUserId") ?? "";
            var email = HttpContext.Session.GetString("VerifyEmail") ?? "";
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Register");
            return View(new VerifyOtpViewModel { UserId = userId, Email = email });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            var userId = HttpContext.Session.GetString("VerifyUserId") ?? model.UserId;
            model.Email = HttpContext.Session.GetString("VerifyEmail") ?? model.Email;

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) { ModelState.AddModelError("", "Session expired. Please register again."); return View(model); }

            if (user.EmailOtp != model.OtpCode)
            { ModelState.AddModelError("OtpCode", "Incorrect code. Check your email and try again."); return View(model); }

            if (user.EmailOtpExpiry < DateTime.Now)
            { ModelState.AddModelError("OtpCode", "Code expired. Click Resend to get a new one."); return View(model); }

            user.IsEmailVerified = true; user.EmailConfirmed = true;
            user.EmailOtp = null; user.EmailOtpExpiry = null;
            await _userManager.UpdateAsync(user);

            HttpContext.Session.Remove("VerifyUserId");
            HttpContext.Session.Remove("VerifyEmail");

            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp()
        {
            var userId = HttpContext.Session.GetString("VerifyUserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Register");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Register");

            var otp = new Random().Next(100000, 999999).ToString();
            user.EmailOtp = otp; user.EmailOtpExpiry = DateTime.Now.AddMinutes(10);
            await _userManager.UpdateAsync(user);

            var sent = await _emailService.SendOtpEmailAsync(user.Email!, user.FullName, otp);
            TempData[sent ? "Success" : "Error"] = sent
                ? "A new code has been sent to your email."
                : $"Email failed. Dev OTP: {otp}";

            return RedirectToAction("VerifyOtp");
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet, Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var customer = _context.Customers.FirstOrDefault(c => c.UserId == user.Id);
            ViewBag.Customer = customer;

            return View(user);
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string phone, string address, string licenseNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.Phone = phone;
            user.Address = address;
            user.LicenseNumber = licenseNumber;
            await _userManager.UpdateAsync(user);

            var customer = _context.Customers.FirstOrDefault(c => c.UserId == user.Id);
            if (customer != null)
            {
                customer.Phone = phone;
                customer.Address = address;
                customer.LicenseNumber = licenseNumber;
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }
        [HttpGet, Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost, Authorize, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Your password has been updated successfully!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter your email address.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var otp = new Random().Next(100000, 999999).ToString();
                user.EmailOtp = otp;
                user.EmailOtpExpiry = DateTime.Now.AddMinutes(15);
                await _userManager.UpdateAsync(user);

                var sent = await _emailService.SendOtpEmailAsync(user.Email!, user.FullName ?? "User", otp);

                HttpContext.Session.SetString("ResetEmail", user.Email!);

                TempData["Success"] = "A 6-digit reset code has been sent to your email.";
                return RedirectToAction("ResetPassword");
            }
            else
            {
                TempData["Success"] = "If that email is registered, you will receive a reset code shortly.";
            }

            return View();
        }
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = HttpContext.Session.GetString("ResetEmail");

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            return View(new ResetPasswordViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("Login");

            if (user.EmailOtp != model.OtpCode || user.EmailOtpExpiry < DateTime.Now)
            {
                ModelState.AddModelError("", "Invalid or expired OTP code.");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                user.EmailOtp = null;
                user.EmailOtpExpiry = null;
                await _userManager.UpdateAsync(user);

                HttpContext.Session.Remove("ResetEmail");
                TempData["Success"] = "Password reset successful! Please login with your new password.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");

        }
    }
}
        
    
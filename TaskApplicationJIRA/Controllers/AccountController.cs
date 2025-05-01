using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskApplicationJIRA.Models.UserModel;
using TaskApplicationJIRA.Data;

namespace TaskApplicationJIRA.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= Register (GET) =================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ================= Register (POST) =================
        [HttpPost]
        public async Task<IActionResult> Register(User model)
        {
            // Check if the email is "admin@cozentus.com" and assign the "Admin" role
            if (model.Email == "admin@cozentus.com")
            {
                model.Role = "Admin";
                
                
            }
            if (ModelState.IsValid)
            {
                Console.WriteLine("Flow reached here");
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "User already exists with this email.");
                    return View(model);
                }

                // Directly save password (⚠️ only for local testing)
                model.CreatedOn = DateTime.Now;
                model.CreatedBy = model.UserId;

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                return RedirectToAction("Login", "Account");
            }
            // If ModelState is invalid, capture errors and pass them back to the view
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                // You can also log these errors or pass them to a ViewBag if needed
                Console.WriteLine(error.ErrorMessage);
            }


            return View(model);
        }

        // ================= Login (GET) =================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ================= Login (POST) =================
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                // Match password directly (⚠️ no hash for local testing)
                if (user == null || user.Password != password)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View();
                }

                // Create Claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                // Redirect based on role
                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Admin"); // Redirect to Admin controller
                }
                else if (user.Role == "Scrum Master")
                {
                    return RedirectToAction("Index", "ScrumMaster"); // Redirect to ScrumMaster controller
                }
                else if (user.Role == "Developer")
                {
                    return RedirectToAction("Index", "Developer"); // Redirect to Developer dashboard or other action
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            return View();
        }


        // ================= Logout =================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolApp.DTO;
using SchoolApp.Services;
using System.Security.Claims;

namespace SchoolApp.Controllers
{
    public class UserController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly ILogger<UserController> _logger;

        public UserController(IApplicationService applicationService, ILogger<UserController> logger)
        {
            _applicationService = applicationService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        [AllowAnonymous]
        public IActionResult Index()
        {

            if (User.IsInRole("ADMIN"))
            {
                return RedirectToAction("Index", "Admin");

            }
            else if (User.IsInRole("TEACHER"))
            {
                return RedirectToAction("Index", "Teacher");
            }
            else if (User.IsInRole("STUDENT"))
            {
                return RedirectToAction("Index", "Student");
            }
            else
            {
                return RedirectToAction("AccessDenied", "Home");
            }
        }


        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            ClaimsPrincipal? principal = HttpContext.User;

            if (!principal.Identity!.IsAuthenticated)
            {
                return View();
            }

            return RedirectToAction("Index", "User");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(UserLoginDTO credentials)
        {
            if (!ModelState.IsValid)
            {
                return View(credentials);
            }

            try
            {
                var user = await _applicationService.UserService.VerifyAndGetUserAsync(credentials);

                if (user is null)
                {
                    ViewData["ValidateMessage"] = "Bad Credentials. Username or password is invalid";
                    return View(credentials);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role.Name)
                };

                ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                AuthenticationProperties properties = new()
                {
                    AllowRefresh = true,
                    IsPersistent = credentials.KeepLoggedIn
                };

                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    properties);

                return RedirectToAction("Index", "User");
            }
            catch (Exception ex)
            {
                ViewData["ValidateMessage"] = ex.Message;
                return View(credentials);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {

            var username = User.Identity!.Name;
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {Username} logged out successfully.", username);
            return RedirectToAction("Login", "User");
        }
    }
}

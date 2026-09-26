using ASI.Basecode.Data;
using ASI.Basecode.Data.Models;
using ASI.Basecode.WebApp.Models;
using ASI.Basecode.WebApp.Mvc;
using ASI.Basecode.WebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace ASI.Basecode.WebApp.Controllers
{
    public class AccountController : ControllerBase<AccountController>
    {
        private const string InvalidLoginMessage =
            "Invalid user code/email or password.";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AsiBasecodeDBContext _dbContext;
        private readonly IBrevoEmailSender _emailSender;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

        private const string PasswordResetOtpProvider = "Gearantee";
        private const string PasswordResetOtpName = "PasswordResetOtp";
        private const int PasswordResetOtpLifetimeMinutes = 10;
        private const int MaximumPasswordResetOtpAttempts = 5;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AsiBasecodeDBContext dbContext,
            IBrevoEmailSender emailSender,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IHttpContextAccessor httpContextAccessor,
            ILoggerFactory loggerFactory,
            IConfiguration configuration)
            : base(
                httpContextAccessor,
                loggerFactory,
                configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _emailSender = emailSender;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && user.IsActive)
            {
                var otp = RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString("D6", CultureInfo.InvariantCulture);
                var expiresAt = DateTimeOffset.UtcNow
                    .AddMinutes(PasswordResetOtpLifetimeMinutes);
                var otpHash = _passwordHasher.HashPassword(user, otp);
                var storedOtp = string.Join(
                    "|",
                    expiresAt.ToUnixTimeSeconds()
                        .ToString(CultureInfo.InvariantCulture),
                    0.ToString(CultureInfo.InvariantCulture),
                    otpHash);

                await _userManager.SetAuthenticationTokenAsync(
                    user,
                    PasswordResetOtpProvider,
                    PasswordResetOtpName,
                    storedOtp);

                try
                {
                    await _emailSender.SendPasswordResetOtpAsync(
                        user.Email ?? email,
                        $"{user.FirstName} {user.LastName}".Trim(),
                        otp);
                }
                catch (Exception exception)
                {
                    await _userManager.RemoveAuthenticationTokenAsync(
                        user,
                        PasswordResetOtpProvider,
                        PasswordResetOtpName);
                    _logger.LogError(
                        exception,
                        "Unable to send a password reset OTP for user {UserCode}.",
                        user.UserCode);
                    ModelState.AddModelError(
                        string.Empty,
                        "We could not send the reset email right now. Please try again later.");
                    return View(model);
                }

                _logger.LogInformation(
                    "Password reset OTP sent for user {UserCode}.",
                    user.UserCode);
            }

            TempData["PasswordResetEmail"] = email;
            return RedirectToAction(nameof(ForgotPasswordCheckInbox));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordCheckInbox()
        {
            if (TempData["PasswordResetEmail"] is not string email ||
                string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new VerifyPasswordResetOtpViewModel { Email = email });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPasswordResetOtp(
            VerifyPasswordResetOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(nameof(ForgotPasswordCheckInbox), model);
            }

            var email = model.Email.Trim();
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The verification code is invalid or has expired.");
                return View(nameof(ForgotPasswordCheckInbox), model);
            }

            var storedOtp = await _userManager.GetAuthenticationTokenAsync(
                user,
                PasswordResetOtpProvider,
                PasswordResetOtpName);
            if (!TryParsePasswordResetOtp(
                    storedOtp,
                    out var expiresAt,
                    out var attempts,
                    out var otpHash) ||
                expiresAt <= DateTimeOffset.UtcNow)
            {
                await _userManager.RemoveAuthenticationTokenAsync(
                    user,
                    PasswordResetOtpProvider,
                    PasswordResetOtpName);
                ModelState.AddModelError(
                    string.Empty,
                    "The verification code is invalid or has expired.");
                return View(nameof(ForgotPasswordCheckInbox), model);
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                otpHash,
                model.Otp.Trim());
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                attempts++;
                if (attempts >= MaximumPasswordResetOtpAttempts)
                {
                    await _userManager.RemoveAuthenticationTokenAsync(
                        user,
                        PasswordResetOtpProvider,
                        PasswordResetOtpName);
                }
                else
                {
                    await _userManager.SetAuthenticationTokenAsync(
                        user,
                        PasswordResetOtpProvider,
                        PasswordResetOtpName,
                        string.Join(
                            "|",
                            expiresAt.ToUnixTimeSeconds()
                                .ToString(CultureInfo.InvariantCulture),
                            attempts.ToString(CultureInfo.InvariantCulture),
                            otpHash));
                }

                ModelState.AddModelError(
                    string.Empty,
                    attempts >= MaximumPasswordResetOtpAttempts
                        ? "Too many attempts. Request a new verification code."
                        : "The verification code is invalid.");
                return View(nameof(ForgotPasswordCheckInbox), model);
            }

            await _userManager.RemoveAuthenticationTokenAsync(
                user,
                PasswordResetOtpProvider,
                PasswordResetOtpName);

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            return RedirectToAction(
                nameof(ResetPassword),
                new { userId = user.Id, token = resetToken });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This reset link is invalid or has expired.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(
                user,
                model.Token,
                model.Password);
            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(model);
            }

            TempData["SuccessMessage"] =
                "Your password was reset. You can now sign in with your new password.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var login = model.UserId?.Trim();
            var normalizedUserName = _userManager.NormalizeName(login);
            var normalizedEmail = _userManager.NormalizeEmail(login);
            var user = await _userManager.Users.FirstOrDefaultAsync(x =>
                x.NormalizedUserName == normalizedUserName ||
                x.NormalizedEmail == normalizedEmail ||
                x.UserCode == login);

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, InvalidLoginMessage);
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Too many failed attempts. Your account is locked for 15 minutes.");
                return View(model);
            }

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, InvalidLoginMessage);
                return View(model);
            }

            _logger.LogInformation("User {UserCode} signed in.", user.UserCode);

            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null ||
                await _userManager.Users.AnyAsync(x =>
                    x.UserCode == model.UserCode))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "An account with that email or user code already exists.");
                return View(model);
            }

            await using var transaction =
                await _dbContext.Database.BeginTransactionAsync();

            var user = new ApplicationUser
            {
                UserName = model.UserCode,
                UserCode = model.UserCode,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(
                user,
                model.Password);
            if (!createResult.Succeeded)
            {
                AddIdentityErrors(createResult);
                await transaction.RollbackAsync();
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(
                DomainValues.Roles.Borrower))
            {
                var createRoleResult = await _roleManager.CreateAsync(
                    new IdentityRole(DomainValues.Roles.Borrower));
                if (!createRoleResult.Succeeded)
                {
                    AddIdentityErrors(createRoleResult);
                    await transaction.RollbackAsync();
                    return View(model);
                }
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                DomainValues.Roles.Borrower);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                await transaction.RollbackAsync();
                return View(model);
            }

            _dbContext.BorrowerProfiles.Add(new BorrowerProfile
            {
                UserId = user.Id,
                SchoolId = model.UserCode,
                Department = model.Department,
                ContactNumber = model.ContactNumber,
                IsEligible = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                "Account created. An administrator must confirm borrowing eligibility.";
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOutUser()
        {
            var userCode = User.FindFirst("user_code")?.Value ?? User.Identity?.Name;
            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();
            _logger.LogInformation("User {UserCode} signed out.", userCode);
            return RedirectToAction(nameof(Login));
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private static bool TryParsePasswordResetOtp(
            string storedOtp,
            out DateTimeOffset expiresAt,
            out int attempts,
            out string otpHash)
        {
            expiresAt = default;
            attempts = 0;
            otpHash = null;

            var parts = storedOtp?.Split('|', 3);
            if (parts == null ||
                parts.Length != 3 ||
                !long.TryParse(
                    parts[0],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var expirySeconds) ||
                !int.TryParse(
                    parts[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out attempts) ||
                attempts < 0 ||
                string.IsNullOrWhiteSpace(parts[2]))
            {
                return false;
            }

            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expirySeconds);
            otpHash = parts[2];
            return true;
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobRequire.Data;
using JobRequire.Models;
using JobRequire.Services;
using JobRequire.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace JobRequire.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly INotificationService _notificationService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<AccountController> _logger;

        // Lockout settings
        private const int MaxFailedAttempts = 3;
        private const int LockoutMinutes = 15;
        
        // Email Activation Link settings (2 minutes expiry)
        private const int ActivationLinkExpiryMinutes = 2;
        
        // OTP settings (kept for password reset)
        private const int OtpExpiryMinutes = 10;

        public AccountController(
            ApplicationDbContext context,
            IPasswordHasher passwordHasher,
            INotificationService notificationService,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _notificationService = notificationService;
            _emailSender = emailSender;
            _logger = logger;
        }

        #region Register

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register(string? role)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel { Role = role ?? "JobSeeker" });
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Validate role-specific fields
            if (model.Role == "Employer" && string.IsNullOrWhiteSpace(model.CompanyName))
            {
                ModelState.AddModelError("CompanyName", "Company name is required for employer registration");
            }
            if (model.Role == "JobSeeker" && string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError("FullName", "Full name is required for job seeker registration");
            }

            if (ModelState.IsValid)
            {
                // Check if email already exists (use lowercase for comparison)
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email.ToLowerInvariant());

                if (existingUserByEmail != null)
                {
                    if (existingUserByEmail.IsActive)
                    {
                        // User is verified and active - block registration
                        ModelState.AddModelError("Email", "An account with this email already exists");
                        return View(model);
                    }
                    else
                    {
                        // User exists but is NOT active (email not verified yet)
                        // Generate new activation link and send email
                        var activationToken = GenerateSecureToken();

                        // Invalidate any existing verification tokens for this user
                        var existingTokens = await _context.EmailVerificationTokens
                            .Where(t => t.UserId == existingUserByEmail.UserId && !t.IsUsed)
                            .ToListAsync();
                        foreach (var oldToken in existingTokens)
                        {
                            oldToken.IsUsed = true;
                        }

                        // Create new verification token with activation link (2 minute expiry)
                        var verificationToken = new EmailVerificationToken
                        {
                            UserId = existingUserByEmail.UserId,
                            Token = activationToken,
                            ExpiryTime = DateTime.UtcNow.AddMinutes(ActivationLinkExpiryMinutes),
                            IsUsed = false
                        };
                        _context.EmailVerificationTokens.Add(verificationToken);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Resending activation link to unverified user {existingUserByEmail.Email}");

                        // Send activation link email
                        try
                        {
                            var activationLink = Url.Action("VerifyEmail", "Account", 
                                new { token = activationToken }, Request.Scheme);
                            await _emailSender.SendEmailActivationLinkAsync(
                                existingUserByEmail.Email,
                                existingUserByEmail.Username,
                                activationLink!
                            );
                            _logger.LogInformation($"Activation link sent to {existingUserByEmail.Email}");
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, $"Failed to send activation email to {existingUserByEmail.Email}");
                        }

                        // Show confirmation page
                        TempData["SuccessMessage"] = "An activation link has been sent to your email. Please check your inbox and click the link to activate your account.";
                        TempData["ActivationEmail"] = existingUserByEmail.Email;
                        return RedirectToAction(nameof(ActivationSent));
                    }
                }

                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "This username is already taken");
                    return View(model);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create user - NOT active until email is verified
                    var user = new User
                    {
                        Username = model.Username,
                        Email = model.Email.ToLowerInvariant(),
                        PasswordHash = _passwordHasher.HashPassword(model.Password),
                        Role = model.Role,
                        IsActive = false, // User is NOT active until email is verified
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Create associated profile - MUST be created for JobSeeker
                    if (model.Role == "Employer")
                    {
                        var employerProfile = new EmployerProfile
                        {
                            UserId = user.UserId,
                            CompanyName = model.CompanyName!
                        };
                        _context.EmployerProfiles.Add(employerProfile);
                    }
                    else if (model.Role == "JobSeeker")
                    {
                        // Ensure FullName is not null or empty - this is required
                        if (string.IsNullOrWhiteSpace(model.FullName))
                        {
                            ModelState.AddModelError("FullName", "Full name is required for job seeker registration");
                            await transaction.RollbackAsync();
                            return View(model);
                        }

                        // ALWAYS create JobSeekerProfile - this is critical for dashboard to work
                        var jobSeekerProfile = new JobSeekerProfile
                        {
                            UserId = user.UserId,
                            FullName = model.FullName.Trim(), // Required field - must not be null
                            PhoneNumber = !string.IsNullOrWhiteSpace(model.PhoneNumber) ? model.PhoneNumber.Trim() : null,
                            Summary = !string.IsNullOrWhiteSpace(model.Summary) ? model.Summary.Trim() : null
                            // ProfilePhoto is not set during registration - can be added later
                        };
                        _context.JobSeekerProfiles.Add(jobSeekerProfile);
                        
                        // Verify the profile was added (defensive check)
                        if (jobSeekerProfile.UserId == 0)
                        {
                            _logger.LogError($"Failed to create JobSeekerProfile for user {user.UserId}");
                            ModelState.AddModelError("", "Failed to create profile. Please try again.");
                            await transaction.RollbackAsync();
                            return View(model);
                        }
                    }

                    // Generate secure activation token
                    var activationToken = GenerateSecureToken();
                    
                    // Create new verification token with activation link (2 minute expiry)
                    var verificationToken = new EmailVerificationToken
                    {
                        UserId = user.UserId,
                        Token = activationToken,
                        ExpiryTime = DateTime.UtcNow.AddMinutes(ActivationLinkExpiryMinutes),
                        IsUsed = false
                    };
                    _context.EmailVerificationTokens.Add(verificationToken);

                    // Log activity
                    var activityLog = new ActivityLog
                    {
                        UserId = user.UserId,
                        Action = "UserRegistered",
                        Timestamp = DateTime.UtcNow,
                        Details = $"User registered as {model.Role} - pending email activation"
                    };
                    _context.ActivityLogs.Add(activityLog);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"User {user.Username} registered successfully - pending email activation");

                    // Send activation link email (outside transaction, don't fail registration if email fails)
                    try
                    {
                        var activationLink = Url.Action("VerifyEmail", "Account", 
                            new { token = activationToken }, Request.Scheme);
                        await _emailSender.SendEmailActivationLinkAsync(
                            user.Email,
                            model.Role == "Employer" ? model.CompanyName! : model.FullName!,
                            activationLink!
                        );
                        _logger.LogInformation($"Activation link sent to {user.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        // Log but don't fail registration if email fails
                        _logger.LogWarning(emailEx, $"Failed to send activation email to {user.Email}, but registration succeeded");
                    }

                    // Redirect to activation sent confirmation page
                    TempData["SuccessMessage"] = "Registration successful! Please check your email and click the activation link to complete registration.";
                    TempData["ActivationEmail"] = user.Email;
                    return RedirectToAction(nameof(ActivationSent));
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    
                    // Check for unique constraint violations (duplicate username/email)
                    if (ex.InnerException is SqlException sqlEx)
                    {
                        // SQL Server unique constraint violation
                        if (sqlEx.Number == 2627 || sqlEx.Number == 2601)
                        {
                            var errorMessage = sqlEx.Message.ToLowerInvariant();
                            
                            if (errorMessage.Contains("username") || errorMessage.Contains("ix_users_username"))
                            {
                                _logger.LogWarning($"Registration failed: Duplicate username '{model.Username}'");
                                ModelState.AddModelError("Username", "This username is already taken");
                            }
                            else if (errorMessage.Contains("email") || errorMessage.Contains("ix_users_email"))
                            {
                                _logger.LogWarning($"Registration failed: Duplicate email '{model.Email}'");
                                ModelState.AddModelError("Email", "An account with this email already exists");
                            }
                            else
                            {
                                _logger.LogError(ex, $"Registration failed: Database constraint violation for user '{model.Username}'");
                                ModelState.AddModelError("", "A user with this information already exists. Please check your username and email.");
                            }
                        }
                        else
                        {
                            _logger.LogError(ex, $"Registration failed: SQL error {sqlEx.Number} for user '{model.Username}'");
                            ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                        }
                    }
                    else
                    {
                        _logger.LogError(ex, $"Registration failed: Database update error for user '{model.Username}'");
                        ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, $"Registration failed: Unexpected error for user '{model.Username}'");
                    ModelState.AddModelError("", "An unexpected error occurred during registration. Please try again.");
                }
            }

            return View(model);
        }

        // GET: Account/RegisterConfirmation (kept for backward compatibility)
        [HttpGet]
        public IActionResult RegisterConfirmation()
        {
            // Redirect to home
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Email Activation Link Verification

        // GET: Account/ActivationSent - Confirmation page after registration
        [HttpGet]
        public IActionResult ActivationSent()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var email = TempData["ActivationEmail"] as string;
            TempData.Keep("ActivationEmail");
            
            ViewBag.Email = email;
            return View();
        }

        // GET: Account/VerifyEmail - Verify email via activation link
        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new VerifyEmailViewModel();

            if (string.IsNullOrEmpty(token))
            {
                viewModel.Success = false;
                viewModel.Message = "Invalid activation link.";
                return View(viewModel);
            }

            // Find the verification token
            var verificationToken = await _context.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);

            if (verificationToken == null)
            {
                viewModel.Success = false;
                viewModel.Message = "Invalid or already used activation link.";
                return View(viewModel);
            }

            // Check if token has expired (2 minutes)
            if (verificationToken.ExpiryTime < DateTime.UtcNow)
            {
                viewModel.Success = false;
                viewModel.Message = "This activation link has expired. Please request a new one.";
                viewModel.ShowResendButton = true;
                viewModel.Email = verificationToken.User.Email;
                return View(viewModel);
            }

            var user = verificationToken.User;

            // Check if already verified
            if (user.IsActive)
            {
                viewModel.Success = true;
                viewModel.Message = "Your email is already verified. You can now login.";
                return View(viewModel);
            }

            // Mark token as used
            verificationToken.IsUsed = true;

            // Activate user account
            user.IsActive = true;

            // Log activity
            var activityLog = new ActivityLog
            {
                UserId = user.UserId,
                Action = "EmailVerified",
                Timestamp = DateTime.UtcNow,
                Details = "Email address verified via activation link"
            };
            _context.ActivityLogs.Add(activityLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Email verified for user {user.Username} via activation link");

            // Create welcome notification
            try
            {
                await _notificationService.CreateNotificationWithActivityAsync(
                    user.UserId,
                    "Welcome to JobRecruit!",
                    "Your email has been verified and your account is now active. You can now start using the platform.",
                    "Success",
                    null,
                    null,
                    "EmailVerified",
                    "Email verified and account activated"
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to create welcome notification for user {user.UserId}");
            }

            viewModel.Success = true;
            viewModel.Message = "Your email has been verified successfully! You can now login to your account.";
            return View(viewModel);
        }

        // POST: Account/ResendActivation - Resend activation email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendActivation(string email)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Email address is required.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());

            if (user == null)
            {
                // Don't reveal if user exists or not
                TempData["SuccessMessage"] = "If an account with that email exists and is not yet activated, a new activation link has been sent.";
                TempData["ActivationEmail"] = email;
                return RedirectToAction(nameof(ActivationSent));
            }

            // Check if already verified
            if (user.IsActive)
            {
                TempData["SuccessMessage"] = "Your account is already activated. Please login.";
                return RedirectToAction(nameof(Login));
            }

            // Generate new activation token
            var activationToken = GenerateSecureToken();

            // Invalidate existing tokens
            var existingTokens = await _context.EmailVerificationTokens
                .Where(t => t.UserId == user.UserId && !t.IsUsed)
                .ToListAsync();
            foreach (var oldToken in existingTokens)
            {
                oldToken.IsUsed = true;
            }

            // Create new token with 2-minute expiry
            var verificationToken = new EmailVerificationToken
            {
                UserId = user.UserId,
                Token = activationToken,
                ExpiryTime = DateTime.UtcNow.AddMinutes(ActivationLinkExpiryMinutes),
                IsUsed = false
            };
            _context.EmailVerificationTokens.Add(verificationToken);

            // Log activity
            var activityLog = new ActivityLog
            {
                UserId = user.UserId,
                Action = "ActivationLinkResent",
                Timestamp = DateTime.UtcNow,
                Details = "New activation link requested"
            };
            _context.ActivityLogs.Add(activityLog);

            await _context.SaveChangesAsync();

            // Send new activation email
            try
            {
                var activationLink = Url.Action("VerifyEmail", "Account", 
                    new { token = activationToken }, Request.Scheme);
                await _emailSender.SendEmailActivationLinkAsync(user.Email, user.Username, activationLink!);
                TempData["SuccessMessage"] = "A new activation link has been sent to your email. Please check your inbox.";
                _logger.LogInformation($"Activation link resent to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to send activation email to {user.Email}");
                TempData["ErrorMessage"] = "Failed to send activation email. Please try again.";
            }

            TempData["ActivationEmail"] = user.Email;
            return RedirectToAction(nameof(ActivationSent));
        }

        #endregion

        #region Login / Logout

        // GET: Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // Find user by email or username
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.EmailOrUsername.ToLowerInvariant() ||
                                              u.Username == model.EmailOrUsername);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email/username or password");
                    return View(model);
                }

                // Check lockout
                var lockoutInfo = await CheckLockout(user.UserId);
                if (lockoutInfo.IsLockedOut)
                {
                    ModelState.AddModelError("", $"Your account is temporarily locked. Please try again in {lockoutInfo.LockoutMinutes} minutes.");
                    return View(model);
                }

                // Verify password
                if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    await RecordFailedLogin(user.UserId);
                    
                    var newLockoutInfo = await CheckLockout(user.UserId);
                    if (newLockoutInfo.IsLockedOut)
                    {
                        ModelState.AddModelError("", $"Too many failed attempts. Your account is locked for {LockoutMinutes} minutes.");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Invalid email/username or password. {newLockoutInfo.AttemptsRemaining} attempts remaining.");
                    }
                    return View(model);
                }

                // Check if account is active (IsActive = false means email not verified OR account deactivated)
                if (!user.IsActive)
                {
                    // Check if there are any used verification tokens - if so, account was deactivated
                    var hasVerifiedBefore = await _context.EmailVerificationTokens
                        .AnyAsync(t => t.UserId == user.UserId && t.IsUsed);
                    
                    if (hasVerifiedBefore)
                    {
                        // Account was previously verified but now deactivated
                        _logger.LogWarning($"Login attempt for deactivated account: {user.Username}");
                        ModelState.AddModelError("", "Your account has been deactivated. Please contact support.");
                        return View(model);
                    }
                    
                    // Email not verified yet - show message with option to resend activation
                    _logger.LogWarning($"Login attempt for unverified email: {user.Username}");
                    
                    // Store email for resend functionality
                    TempData["UnverifiedEmail"] = user.Email;
                    ModelState.AddModelError("", "Please activate your account via the email sent to you.");
                    return View(model);
                }

                // Clear failed login attempts
                await ClearFailedLogins(user.UserId);

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Log activity
                var activityLog = new ActivityLog
                {
                    UserId = user.UserId,
                    Action = "UserLogin",
                    Timestamp = DateTime.UtcNow,
                    Details = $"User logged in from {HttpContext.Connection.RemoteIpAddress}"
                };
                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Username} logged in");

                // Redirect to return URL or appropriate dashboard
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return user.Role switch
                {
                    "Admin" => RedirectToAction("Index", "Admin"),
                    "Employer" => RedirectToAction("Dashboard", "Employer"),
                    "JobSeeker" => RedirectToAction("Dashboard", "JobSeekerProfiles"),
                    _ => RedirectToAction("Index", "Home")
                };
            }

            return View(model);
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userId, out int uid))
            {
                var activityLog = new ActivityLog
                {
                    UserId = uid,
                    Action = "UserLogout",
                    Timestamp = DateTime.UtcNow,
                    Details = "User logged out"
                };
                _context.ActivityLogs.Add(activityLog);
                await _context.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation($"User logged out");

            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Forgot / Reset Password

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email.ToLowerInvariant());

                // Only process if user exists - but always show same message to prevent email enumeration
                if (user != null)
                {
                    // Invalidate old tokens
                    var oldTokens = await _context.PasswordResetTokens
                        .Where(t => t.UserId == user.UserId && !t.IsUsed)
                        .ToListAsync();

                    foreach (var oldToken in oldTokens)
                    {
                        oldToken.IsUsed = true;
                    }

                    // Generate 6-digit OTP code
                    var otpCode = GenerateOtpCode();
                    var resetToken = new PasswordResetToken
                    {
                        UserId = user.UserId,
                        Token = otpCode,
                        ExpiryTime = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                        IsUsed = false
                    };
                    _context.PasswordResetTokens.Add(resetToken);

                    // Log activity
                    var activityLog = new ActivityLog
                    {
                        UserId = user.UserId,
                        Action = "PasswordResetRequested",
                        Timestamp = DateTime.UtcNow,
                        Details = "Password reset OTP requested"
                    };
                    _context.ActivityLogs.Add(activityLog);

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Password reset OTP generated for {user.Email}");

                    // Send password reset OTP email
                    try
                    {
                        await _emailSender.SendPasswordResetOtpAsync(user.Email, user.Username, otpCode);
                        _logger.LogInformation($"Password reset OTP email sent to {user.Email}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to send password reset OTP email to {user.Email}");
                    }

                    // Store email in TempData for the verification page
                    TempData["ResetEmail"] = user.Email;
                    
                    // Redirect to OTP verification page
                    TempData["SuccessMessage"] = "A verification code has been sent to your email.";
                    return RedirectToAction(nameof(VerifyResetCode), new { email = user.Email });
                }

                // For non-existent users, show generic confirmation to prevent email enumeration
                TempData["SuccessMessage"] = "If an account with that email exists, a verification code has been sent.";
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            return View(model);
        }

        // GET: Account/ForgotPasswordConfirmation
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: Account/VerifyResetCode
        [HttpGet]
        public IActionResult VerifyResetCode(string email)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new VerifyResetCodeViewModel { Email = email });
        }

        // POST: Account/VerifyResetCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyResetCode(VerifyResetCodeViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email.ToLowerInvariant());

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid verification request.");
                return View(model);
            }

            // Find valid OTP token
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.UserId && 
                                          t.Token == model.OtpCode && 
                                          !t.IsUsed);

            if (resetToken == null)
            {
                ModelState.AddModelError("OtpCode", "Invalid verification code.");
                return View(model);
            }

            if (resetToken.ExpiryTime < DateTime.UtcNow)
            {
                ModelState.AddModelError("OtpCode", "Verification code has expired. Please request a new one.");
                return View(model);
            }

            // Store verification state in TempData for the ResetPassword page
            // Using a secure token to prevent direct access to reset page without verification
            var verificationKey = GenerateSecureToken();
            TempData["ResetVerified"] = verificationKey;
            TempData["ResetEmail"] = model.Email;
            TempData["ResetTokenId"] = resetToken.PasswordResetTokenId;

            _logger.LogInformation($"Password reset OTP verified for {user.Email}");

            return RedirectToAction(nameof(ResetPassword), new { email = model.Email, key = verificationKey });
        }

        // POST: Account/ResendResetCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendResetCode(ResendResetCodeViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email.ToLowerInvariant());

            if (user == null)
            {
                // Don't reveal that user doesn't exist
                TempData["SuccessMessage"] = "If an account with that email exists, a new verification code has been sent.";
                return RedirectToAction(nameof(VerifyResetCode), new { email = model.Email });
            }

            // Invalidate existing tokens
            var existingTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.UserId && !t.IsUsed)
                .ToListAsync();
            foreach (var oldToken in existingTokens)
            {
                oldToken.IsUsed = true;
            }

            // Generate new OTP
            var otpCode = GenerateOtpCode();
            var resetToken = new PasswordResetToken
            {
                UserId = user.UserId,
                Token = otpCode,
                ExpiryTime = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
                IsUsed = false
            };
            _context.PasswordResetTokens.Add(resetToken);

            // Log activity
            var activityLog = new ActivityLog
            {
                UserId = user.UserId,
                Action = "PasswordResetOtpResent",
                Timestamp = DateTime.UtcNow,
                Details = "New password reset verification code requested"
            };
            _context.ActivityLogs.Add(activityLog);

            await _context.SaveChangesAsync();

            // Send new OTP email
            try
            {
                await _emailSender.SendPasswordResetOtpAsync(user.Email, user.Username, otpCode);
                TempData["SuccessMessage"] = "A new verification code has been sent to your email.";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to send password reset OTP email to {user.Email}");
                TempData["ErrorMessage"] = "Failed to send verification email. Please try again.";
            }

            return RedirectToAction(nameof(VerifyResetCode), new { email = user.Email });
        }

        // GET: Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword(string email, string key)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Verify the reset was properly verified via OTP
            var storedKey = TempData["ResetVerified"] as string;
            var storedEmail = TempData["ResetEmail"] as string;

            // Keep TempData for the POST action
            TempData.Keep("ResetVerified");
            TempData.Keep("ResetEmail");
            TempData.Keep("ResetTokenId");

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(email) ||
                storedKey != key || storedEmail?.ToLowerInvariant() != email.ToLowerInvariant())
            {
                TempData["ErrorMessage"] = "Invalid or expired password reset session. Please start again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            return View(new ResetPasswordViewModel { Email = email });
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            // Verify the reset was properly verified via OTP
            var storedKey = TempData["ResetVerified"] as string;
            var storedEmail = TempData["ResetEmail"] as string;
            var tokenIdObj = TempData["ResetTokenId"];

            if (string.IsNullOrEmpty(storedKey) || string.IsNullOrEmpty(storedEmail) || tokenIdObj == null ||
                storedEmail.ToLowerInvariant() != model.Email.ToLowerInvariant())
            {
                TempData["ErrorMessage"] = "Invalid or expired password reset session. Please start again.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (!ModelState.IsValid)
            {
                // Keep TempData for retry
                TempData["ResetVerified"] = storedKey;
                TempData["ResetEmail"] = storedEmail;
                TempData["ResetTokenId"] = tokenIdObj;
                return View(model);
            }

            var tokenId = Convert.ToInt32(tokenIdObj);
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.PasswordResetTokenId == tokenId && 
                                          t.User.Email == model.Email.ToLowerInvariant());

            if (resetToken == null)
            {
                TempData["ErrorMessage"] = "Invalid password reset request.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (resetToken.IsUsed)
            {
                TempData["ErrorMessage"] = "This password reset code has already been used.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Update password
            resetToken.User.PasswordHash = _passwordHasher.HashPassword(model.Password);
            resetToken.IsUsed = true;

            // Clear any failed login attempts
            await ClearFailedLogins(resetToken.UserId);

            // Log activity
            var activityLog = new ActivityLog
            {
                UserId = resetToken.UserId,
                Action = "PasswordReset",
                Timestamp = DateTime.UtcNow,
                Details = "Password was reset successfully via OTP verification"
            };
            _context.ActivityLogs.Add(activityLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Password reset for user {resetToken.User.Username}");

            TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in.";
            return RedirectToAction(nameof(Login));
        }

        #endregion

        #region Access Denied

        // GET: Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #endregion

        #region Profile / Settings

        // GET: Account/Profile
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return user.Role switch
            {
                "Employer" => RedirectToAction("MyProfile", "EmployerProfiles"),
                "JobSeeker" => RedirectToAction("MyProfile", "JobSeekerProfiles"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        // GET: Account/Settings
        [Authorize]
        public IActionResult Settings()
        {
            return View();
        }

        // GET: Account/ChangePassword
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = GetCurrentUserId();
                if (userId == null) return Unauthorized();

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return NotFound();

                if (!_passwordHasher.VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                    return View(model);
                }

                user.PasswordHash = _passwordHasher.HashPassword(model.NewPassword);

                var activityLog = new ActivityLog
                {
                    UserId = user.UserId,
                    Action = "PasswordChanged",
                    Timestamp = DateTime.UtcNow,
                    Details = "Password changed by user"
                };
                _context.ActivityLogs.Add(activityLog);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction(nameof(Settings));
            }

            return View(model);
        }

        #endregion

        #region Helper Methods

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }

        private static string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        /// <summary>
        /// Generate a 6-digit OTP code for email verification
        /// </summary>
        private static string GenerateOtpCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
            return number.ToString("D6"); // Ensure 6 digits with leading zeros
        }

        private async Task<AccountLockoutInfo> CheckLockout(int userId)
        {
            var failedAttempt = await _context.FailedLoginAttempts
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (failedAttempt == null)
            {
                return new AccountLockoutInfo
                {
                    IsLockedOut = false,
                    AttemptsRemaining = MaxFailedAttempts
                };
            }

            // Check if lockout has expired
            if (failedAttempt.LastAttemptTime.HasValue &&
                failedAttempt.AttemptCount >= MaxFailedAttempts)
            {
                var lockoutEnd = failedAttempt.LastAttemptTime.Value.AddMinutes(LockoutMinutes);
                if (DateTime.UtcNow < lockoutEnd)
                {
                    return new AccountLockoutInfo
                    {
                        IsLockedOut = true,
                        AttemptsRemaining = 0,
                        LockoutEndTime = lockoutEnd,
                        LockoutMinutes = (int)Math.Ceiling((lockoutEnd - DateTime.UtcNow).TotalMinutes)
                    };
                }
                else
                {
                    // Lockout expired, reset
                    failedAttempt.AttemptCount = 0;
                    await _context.SaveChangesAsync();
                }
            }

            return new AccountLockoutInfo
            {
                IsLockedOut = false,
                AttemptsRemaining = MaxFailedAttempts - failedAttempt.AttemptCount
            };
        }

        private async Task RecordFailedLogin(int userId)
        {
            var failedAttempt = await _context.FailedLoginAttempts
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (failedAttempt == null)
            {
                failedAttempt = new FailedLoginAttempt
                {
                    UserId = userId,
                    AttemptCount = 1,
                    LastAttemptTime = DateTime.UtcNow
                };
                _context.FailedLoginAttempts.Add(failedAttempt);
            }
            else
            {
                // If last attempt was more than lockout period ago, reset counter
                if (failedAttempt.LastAttemptTime.HasValue &&
                    failedAttempt.LastAttemptTime.Value.AddMinutes(LockoutMinutes) < DateTime.UtcNow)
                {
                    failedAttempt.AttemptCount = 1;
                }
                else
                {
                    failedAttempt.AttemptCount++;
                }
                failedAttempt.LastAttemptTime = DateTime.UtcNow;
            }

            // Log activity
            var activityLog = new ActivityLog
            {
                UserId = userId,
                Action = "FailedLogin",
                Timestamp = DateTime.UtcNow,
                Details = $"Failed login attempt #{failedAttempt.AttemptCount}"
            };
            _context.ActivityLogs.Add(activityLog);

            await _context.SaveChangesAsync();
        }

        private async Task ClearFailedLogins(int userId)
        {
            var failedAttempt = await _context.FailedLoginAttempts
                .FirstOrDefaultAsync(f => f.UserId == userId);

            if (failedAttempt != null)
            {
                failedAttempt.AttemptCount = 0;
                failedAttempt.LastAttemptTime = null;
                await _context.SaveChangesAsync();
            }
        }

        #endregion
    }
}

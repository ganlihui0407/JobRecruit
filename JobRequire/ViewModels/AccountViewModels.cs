using System.ComponentModel.DataAnnotations;
using JobRequire.Validation;

namespace JobRequire.ViewModels
{
    // =============================================
    // ACCOUNT VIEW MODELS
    // =============================================
    // Purpose: ViewModels for authentication and account management screens.
    // Includes: Registration, Login, Email Verification, Password Reset, Change Password.
    // 
    // Design Decision: Each ViewModel serves exactly ONE form/screen.
    // Validation is done via DataAnnotations with user-friendly error messages.
    // Controllers rely on ModelState.IsValid for validation.
    // =============================================

    /// <summary>
    /// ViewModel for the user registration form.
    /// Supports both Employer and JobSeeker registration with role-specific fields.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Account/Register
    /// Role-specific validation is handled in the controller.
    /// </remarks>
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an account type")]
        [Display(Name = "I am a")]
        public string Role { get; set; } = string.Empty; // "Employer" or "JobSeeker"

        // For Employer registration
        [Display(Name = "Company Name")]
        [StringLength(150)]
        public string? CompanyName { get; set; }

        // For JobSeeker registration (conditionally required - validated in controller)
        [Display(Name = "Full Name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string? FullName { get; set; }

        [Display(Name = "Phone Number")]
        [StringLength(20)]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Professional Summary")]
        [StringLength(2000, ErrorMessage = "Summary cannot exceed 2000 characters")]
        [DataType(DataType.MultilineText)]
        public string? Summary { get; set; }

        [Display(Name = "I agree to the Terms of Service and Privacy Policy")]
        [MustBeTrue(ErrorMessage = "You must agree to the terms")]
        public bool AgreeToTerms { get; set; }
    }

    /// <summary>
    /// ViewModel for the login form.
    /// Supports login via email or username with optional "Remember Me" functionality.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Account/Login
    /// ReturnUrl is used to redirect after successful login.
    /// </remarks>
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email or username is required")]
        [Display(Name = "Email or Username")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    /// <summary>
    /// ViewModel for displaying email verification result.
    /// Used after user clicks the activation link in their email.
    /// </summary>
    /// <remarks>
    /// Usage: GET /Account/VerifyEmail?token=xxx
    /// Shows success/failure message and optional resend button.
    /// </remarks>
    public class VerifyEmailViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool ShowResendButton { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// ViewModel for OTP code verification form.
    /// Legacy support for code-based email verification.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Account/VerifyOtp
    /// Validates 6-digit numeric code.
    /// </remarks>
    public class VerifyOtpViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string OtpCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for resending OTP verification code.
    /// </summary>
    public class ResendOtpViewModel
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for the forgot password form.
    /// User enters their email to receive a password reset link.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Account/ForgotPassword
    /// </remarks>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for verifying password reset code.
    /// Legacy support for OTP-based password reset.
    /// </summary>
    public class VerifyResetCodeViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Verification code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string OtpCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for resending password reset code.
    /// </summary>
    public class ResendResetCodeViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for the password reset form.
    /// User sets a new password after verifying their identity.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Account/ResetPassword
    /// Email is passed from the verification step.
    /// </remarks>
    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for the change password form.
    /// Authenticated users can change their password.
    /// </summary>
    /// <remarks>
    /// Usage: GET/POST /Account/ChangePassword
    /// Requires current password for verification.
    /// </remarks>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your new password")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for account lockout status information.
    /// Used to display lockout warnings on the login page.
    /// </summary>
    public class AccountLockoutInfo
    {
        public bool IsLockedOut { get; set; }
        public int AttemptsRemaining { get; set; }
        public DateTime? LockoutEndTime { get; set; }
        public int LockoutMinutes { get; set; }
    }
}


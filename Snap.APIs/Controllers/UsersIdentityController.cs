using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Snap.APIs.DTOs;
using Snap.APIs.Errors;
using Snap.Core.Entities;
using Snap.Core.Services;
using Snap.Repository.Data;
using System.Collections.Concurrent;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Snap.APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersIdentityController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly SnapDbContext _context;
        private readonly IConfiguration _configuration;
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry, bool Verified)> _otpStore = new();
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);

        public UsersIdentityController(UserManager<User>userManager ,
            SignInManager<User> signInManager , 
            ITokenService tokenService,
            SnapDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("SendOtp")]
        public ActionResult SendOtp([FromBody] SendOtpDto dto)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[dto.PhoneNumber] = (otp, DateTime.UtcNow.Add(OtpLifetime), false);

            // WhatsApp sending logic
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromNumber = _configuration["Twilio:WhatsAppFrom"];
            if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(fromNumber))
            {
                TwilioClient.Init(accountSid, authToken);
                var to = new PhoneNumber($"whatsapp:+2{dto.PhoneNumber}"); // Egypt country code as example
                var from = new PhoneNumber(fromNumber);
                var message = MessageResource.Create(
                    to: to,
                    from: from,
                    body: $"Your OTP is: {otp}"
                );
            }
            return Ok(new { message = $"OTP sent to WhatsApp for {dto.PhoneNumber}" });
        }

        [HttpPost("VerifyOtp")]
        public ActionResult VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            if (!_otpStore.TryGetValue(dto.PhoneNumber, out var entry))
                return BadRequest(new ApiResponse(400, "OTP not requested or expired."));
            if (entry.Expiry < DateTime.UtcNow)
                return BadRequest(new ApiResponse(400, "OTP expired."));
            if (entry.Otp != dto.Otp)
                return BadRequest(new ApiResponse(400, "Invalid OTP."));
            _otpStore[dto.PhoneNumber] = (entry.Otp, entry.Expiry, true);
            return Ok(new { message = "OTP verified." });
        }

        //Register
        [HttpPost("Register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto model)
        {
            if (!_otpStore.TryGetValue(model.PhoneNumber, out var entry) || !entry.Verified)
            {
                return BadRequest(new ApiResponse(400, "Phone number not verified by OTP."));
            }
            var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserByEmail != null)
            {
                return BadRequest(new ApiResponse(400, "An account is already registered with this email."));
            }
            var existingUserByDisplayName = _userManager.Users
                .FirstOrDefault(u => u.FullName.ToLower() == model.FullName.ToLower());
            if (existingUserByDisplayName != null)
            {
                return BadRequest(new ApiResponse(400, "Display name is already taken. Please choose another one."));
            }
            var user = new User()
            {
                FullName = model.FullName,
                Email = model.Email,
                UserName = model.Email.Split('@')[0],
                PhoneNumber = model.PhoneNumber
            };
            var result = await _userManager.CreateAsync(user, model.password);
            if (!result.Succeeded) { return BadRequest(new ApiResponse(400)); }
            var ReturnedUser = new UserDto()
            {
                UserId = user.Id,
                DispalyName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Token = await _tokenService.CreateTokenAsync(user , _userManager)
            };
            _otpStore.TryRemove(model.PhoneNumber, out _);
            return Ok(ReturnedUser);
        }
        //login 
        [HttpPost ("Login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.EmailOrPhone);
            if (user == null)
            {
                user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == model.EmailOrPhone);
            }
            if (user == null)
            {
                return Unauthorized(new ApiResponse(401));
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new ApiResponse(401));
            }
            return Ok(new UserDto
            {
                UserId = user.Id,
                DispalyName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Token = await _tokenService.CreateTokenAsync(user, _userManager)
            });
        }
        [HttpDelete("Delete/{userId}")]
        public async Task<ActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new ApiResponse(404, "User not found."));
            }
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new ApiResponse(400, "Failed to delete user."));
            }
            return Ok(new { message = "User deleted successfully." });
        }

        // Request OTP for password reset
        [HttpPost("RequestResetPasswordOtp")]
        public ActionResult RequestResetPasswordOtp([FromBody] ResetPasswordRequestDto dto)
        {
            var user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == dto.PhoneNumber);
            if (user == null)
                return BadRequest(new ApiResponse(400, "No user found with this phone number."));
            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[$"reset_{dto.PhoneNumber}"] = (otp, DateTime.UtcNow.Add(OtpLifetime), false);
            var accountSid = _configuration["Twilio:AccountSid"];
            var authToken = _configuration["Twilio:AuthToken"];
            var fromNumber = _configuration["Twilio:WhatsAppFrom"];
            if (!string.IsNullOrEmpty(accountSid) && !string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(fromNumber))
            {
                TwilioClient.Init(accountSid, authToken);
                var to = new PhoneNumber($"whatsapp:+2{dto.PhoneNumber}");
                var from = new PhoneNumber(fromNumber);
                var message = MessageResource.Create(
                    to: to,
                    from: from,
                    body: $"Your password reset OTP is: {otp}"
                );
            }
            return Ok(new { message = $"Password reset OTP sent to WhatsApp for {dto.PhoneNumber}" });
        }

        // Verify OTP for password reset
        [HttpPost("VerifyResetPasswordOtp")]
        public ActionResult VerifyResetPasswordOtp([FromBody] ResetPasswordVerifyOtpDto dto)
        {
            if (!_otpStore.TryGetValue($"reset_{dto.PhoneNumber}", out var entry))
                return BadRequest(new ApiResponse(400, "OTP not requested or expired."));
            if (entry.Expiry < DateTime.UtcNow)
                return BadRequest(new ApiResponse(400, "OTP expired."));
            if (entry.Otp != dto.Otp)
                return BadRequest(new ApiResponse(400, "Invalid OTP."));
            _otpStore[$"reset_{dto.PhoneNumber}"] = (entry.Otp, entry.Expiry, true);
            return Ok(new { message = "OTP verified for password reset." });
        }

        // Reset password
        [HttpPost("ResetPassword")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!_otpStore.TryGetValue($"reset_{dto.PhoneNumber}", out var entry) || !entry.Verified)
                return BadRequest(new ApiResponse(400, "OTP not verified for this phone number."));
            var user = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == dto.PhoneNumber);
            if (user == null)
                return BadRequest(new ApiResponse(400, "No user found with this phone number."));
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new ApiResponse(400, "Failed to reset password."));
            _otpStore.TryRemove($"reset_{dto.PhoneNumber}", out _);
            return Ok(new { message = "Password reset successful." });
        }
    }
}
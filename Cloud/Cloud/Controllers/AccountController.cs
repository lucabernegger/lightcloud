﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Cloud.Controllers
{
    [ApiController]
    [ResponseCache(NoStore = true, Duration = 0, VaryByHeader = "*")]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IJwtAuthManager _jwtAuthManager;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
            _jwtAuthManager = new JwtAuthManager(Startup.TokenConfig);
        }

        [AllowAnonymous]
        [HttpPost("get-token")]
        public ActionResult Login([FromQuery] LoginRequest request)
        {
            /*if (!ModelState.IsValid)
            {
                return BadRequest();
            }   */

            if (!UserManager.IsValid(request.UserName, request.Password))
            {
                return Unauthorized();
            }

            var user = UserManager.GetUserFromName(request.UserName);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Role,user.IsAdmin ? "Admin" : String.Empty),
                new Claim(ClaimTypes.Name,user.Name)
            };

            var jwtResult = _jwtAuthManager.GenerateTokens(request.UserName, claims, DateTime.Now);
            _logger.LogInformation($"User [{request.UserName}] logged in the system.");
            return Ok(new LoginResult
            {
                UserName = request.UserName,
                Role = user.IsAdmin ? "Admin" : String.Empty,
                AccessToken = jwtResult.AccessToken,
                RefreshToken = jwtResult.RefreshToken.TokenString
            });
        }
        [HttpPost("refresh-token")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> RefreshToken([FromQuery] RefreshTokenRequest request)
        {
            try
            {
                var userName = User.Identity?.Name;
                _logger.LogInformation($"User [{userName}] is trying to refresh JWT token.");

                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                {
                    return Unauthorized();
                }

                var accessToken = await HttpContext.GetTokenAsync("Bearer", "access_token");
                var jwtResult = _jwtAuthManager.Refresh(request.RefreshToken, accessToken, DateTime.Now);
                _logger.LogInformation($"User [{userName}] has refreshed JWT token.");
                return Ok(new LoginResult
                {
                    UserName = userName,
                    Role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                    AccessToken = jwtResult.AccessToken,
                    RefreshToken = jwtResult.RefreshToken.TokenString
                });
            }
            catch (SecurityTokenException e)
            {
                return Unauthorized(e.Message); // return 401 so that the client side can redirect the user to login page
            }
        }
        [HttpPost("/disable-token")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        public void DisableToken()
        {
            HttpContext.Session.Remove("ServerFileKeyComponent");
            _jwtAuthManager.RemoveRefreshTokenByUserName(this.User.FindFirstValue(ClaimTypes.Name));
        }

        [HttpGet("/Logout")]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("ServerFileKeyComponent");
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Index");
        }

    }
    public class LoginRequest
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class LoginResult
    {
        public string UserName { get; set; }

        public string Role { get; set; }

        public string OriginalUserName { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }

    public class ImpersonationRequest
    {
        public string UserName { get; set; }
    }
}
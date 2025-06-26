using AuthService.Models;
using AuthService.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AuthService.Controllers
{
    [ApiVersion("2.0")]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthV2Controller : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly JwtTokenService _jwtService;

        public AuthV2Controller(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger,
            JwtTokenService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Invalid login request." });

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null || !(await _signInManager.CheckPasswordSignInAsync(user, request.Password, false)).Succeeded)
                return Unauthorized(new { error = "Invalid credentials." });

            var token = _jwtService.GenerateToken(user);
            return Ok(new
            {
                message = "Login successful (v2)",
                token,
                user = new { user.FullName, user.Email }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Invalid registration request." });

            var user = new ApplicationUser
            {
                UserName = request.Email,
                FullName = request.FullName,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

            var token = _jwtService.GenerateToken(user);
            return Ok(new
            {
                message = "Registration successful (v2)",
                token
            });
        }

        [HttpGet("protected")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Protected()
        {
            var userEmail = User?.Identity?.Name ?? "Unknown";
            return Ok(new
            {
                message = "You accessed a protected route in API v2!",
                email = userEmail
            });
        }

        // New v2-only endpoint
        [HttpGet("info")]
        public IActionResult Info()
        {
            return Ok(new
            {
                version = "2.0",
                description = "This endpoint exists only in API version 2."
            });
        }
    }
}

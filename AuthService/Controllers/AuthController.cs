using AuthService.Models;
using AuthService.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;


namespace AuthService.Controllers
{
    [ApiVersion("1.0")]
    [ApiController]
    [Route("v{version:apiVersion}/auth")]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;

        private readonly JwtTokenService _jwtService;


        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AuthController> logger, JwtTokenService jwtService)
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
            {
                _logger.LogWarning("Invalid login attempt with email {Email}.", request.Email);
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null)
            {
                _logger.LogWarning("Invalid login attempt with email {Email}.", request.Email);
                return Unauthorized("Invalid Credentials.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Invalid login attempt with email {Email}.", request.Email);
                return Unauthorized("Invalid Credentials.");
            }

            _logger.LogInformation("User with email {Email} logged in successfully.", request.Email);
            var token = _jwtService.GenerateToken(user);
            return Ok(new { token });

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration attempt with email {Email}.", request.Email);
                return BadRequest(ModelState);
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                FullName = request.FullName,
                Email = request.Email
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Invalid registration attempt with email {Email}. Errors: {Errors}", request.Email, string.Join(", ", result.Errors.Select(e => e.Description)));


                return BadRequest(result.Errors);
            }

            _logger.LogInformation("User with email {Email} registered successfully.", request.Email);
            // JWT token 
            var token = _jwtService.GenerateToken(user);

            Console.WriteLine($"Received: {request.Email} - {request.FullName}");
            Console.WriteLine($"Received: {request.Email} - {request.FullName}");


            return Ok(new { message = "Registration successful", token });
            //return Ok("Registration successful.");
            
        }

        [HttpGet("protected")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult Protected()
        {
            Console.WriteLine(" Protected endpoint hit.");

            var userEmail = User?.Identity?.Name ?? "Unknown";

            return Ok("You accessed a protected route!");
        }

    }
}


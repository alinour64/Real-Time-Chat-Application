using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ChatbotBackend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChatbotBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthenticationController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            _logger.LogInformation("Registering user: {Username}", model.Username);

            var user = new ApplicationUser { UserName = model.Username };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User registered successfully: {Username}", model.Username);
                return Ok(new { Result = "Registration successful" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("User registration failed: {Username}. Errors: {Errors}", model.Username, errors);
            return BadRequest(new { Result = "Registration failed", Errors = errors });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            _logger.LogInformation("User attempting to login: {Username}", model.Username);

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                _logger.LogWarning("User login failed: {Username}. User not found.", model.Username);
                return Unauthorized(new { Result = "Invalid username or password" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (result.Succeeded)
            {
                var token = GenerateJwtToken(user);
                _logger.LogInformation("User logged in successfully: {Username}", model.Username);
                return Ok(new { Token = token });
            }

            if (result.IsNotAllowed)
            {
                _logger.LogWarning("User login failed: {Username}. Not allowed.", model.Username);
                return Unauthorized(new { Result = "Not allowed to login" });
            }

            _logger.LogWarning("User login failed: {Username}. Invalid password.", model.Username);
            return Unauthorized(new { Result = "Invalid username or password" });
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserName))
            {
                throw new ArgumentException("User information is not valid.");
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            if (key.KeySize < 128)
            {
                throw new InvalidOperationException("The JWT key is not sufficiently secure.");
            }

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtIssuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

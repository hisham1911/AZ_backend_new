using Microsoft.AspNetCore.Mvc;
using az_backend_new.Repositories;
using az_backend_new.Services;

namespace az_backend_new.Controllers
{
    /// <summary>
    /// Legacy Account controller for backward compatibility with existing frontend
    /// Note: The typo "Acount" is intentional to match the old API endpoint
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AcountController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AcountController> _logger;

        public AcountController(
            IUserRepository userRepository,
            IJwtService jwtService,
            ILogger<AcountController> logger)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Legacy login endpoint - returns simple text response for backward compatibility
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LegacyLoginDto loginDto)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return Unauthorized("Invalid email or password");
                }

                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return Unauthorized("Invalid email or password");
                }

                // Generate JWT token for the new system
                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("User logged in via legacy API: {Email}", user.Email);

                // Return text response for backward compatibility
                // The frontend checks for "Login successful" text
                return Ok($"Login successful. Token: {token}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during legacy login");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class LegacyLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

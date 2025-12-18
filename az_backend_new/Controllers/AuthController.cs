using Microsoft.AspNetCore.Mvc;
using az_backend_new.DTOs;
using az_backend_new.Models;
using az_backend_new.Repositories;
using az_backend_new.Services;

namespace az_backend_new.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserRepository userRepository,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
        {
            try
            {
                // Check if email already exists
                if (await _userRepository.EmailExistsAsync(registerDto.Email))
                {
                    return Conflict(new { message = "Email already exists" });
                }

                // Create new user
                var user = new User
                {
                    Email = registerDto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    Role = Role.User
                };

                user = await _userRepository.CreateAsync(user);

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(24);

                _logger.LogInformation("User registered successfully: {Email}", user.Email);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    Role = user.Role,
                    ExpiresAt = expiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                // Find user by email
                var user = await _userRepository.GetByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Generate JWT token
                var token = _jwtService.GenerateToken(user);
                var expiresAt = DateTime.UtcNow.AddHours(24);

                _logger.LogInformation("User logged in successfully: {Email}", user.Email);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    Role = user.Role,
                    ExpiresAt = expiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user login");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
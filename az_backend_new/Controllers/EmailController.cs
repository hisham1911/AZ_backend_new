using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using az_backend_new.DTOs;
using az_backend_new.Services;

namespace az_backend_new.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IEmailService emailService,
            ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("SendEmail")]
        [AllowAnonymous]
        public async Task<ActionResult<EmailResponseDto>> SendEmail(SendEmailDto emailDto)
        {
            try
            {
                var result = await _emailService.SendEmailAsync(emailDto);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}

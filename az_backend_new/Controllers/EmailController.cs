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

        // Endpoint متوافق مع الفرونت إند (نفس الباك إند القديم)
        [HttpPost("SendEmail")]
        [AllowAnonymous]
        public async Task<ActionResult<EmailResponseDto>> SendEmail(ContactFormDto contactForm)
        {
            try
            {
                // تحويل ContactFormDto إلى SendEmailDto
                var emailDto = new SendEmailDto
                {
                    To = "info@azinternational-eg.com", // الإيميل المستقبل
                    Subject = contactForm.Subject,
                    Body = $@"
<div>
    <h2>رسالة جديدة من موقع AZ International</h2>
    <p><strong>الاسم:</strong> {contactForm.UserName}</p>
    <p><strong>الإيميل:</strong> {contactForm.UserEmail}</p>
    <p><strong>الموضوع:</strong> {contactForm.Subject}</p>
    <p><strong>الرسالة:</strong></p>
    <p>{contactForm.Message}</p>
</div>",
                    IsHtml = true
                };

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
                _logger.LogError(ex, "Error sending contact form email");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // Endpoint للإرسال المباشر
        [HttpPost("SendDirect")]
        [AllowAnonymous]
        public async Task<ActionResult<EmailResponseDto>> SendDirectEmail(SendEmailDto emailDto)
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

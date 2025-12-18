using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using az_backend_new.DTOs;
using az_backend_new.Services;
using az_backend_new.Repositories;

namespace az_backend_new.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ICertificateRepository _certificateRepository;
        private readonly ILogger<EmailController> _logger;

        public EmailController(
            IEmailService emailService,
            ICertificateRepository certificateRepository,
            ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _certificateRepository = certificateRepository;
            _logger = logger;
        }

        [HttpPost("send")]
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

        [HttpPost("send-expiry-notifications")]
        public async Task<ActionResult<List<EmailResponseDto>>> SendExpiryNotifications()
        {
            try
            {
                var expiringCertificates = await _certificateRepository.GetExpiringCertificatesAsync(30);
                var results = new List<EmailResponseDto>();

                foreach (var certificate in expiringCertificates)
                {
                    var certificateDetails = $@"
Serial Number: {certificate.SerialNumber}
Person Name: {certificate.PersonName}
Service Method: {certificate.ServiceMethod}
Certificate Type: {certificate.CertificateType}
Expiry Date: {certificate.ExpiryDate:yyyy-MM-dd}
Days Until Expiry: {(certificate.ExpiryDate - DateTime.UtcNow).Days}
";

                    // For demo purposes, we'll use a placeholder email
                    // In production, you would have the actual recipient email stored
                    var recipientEmail = "certificate.holder@example.com";
                    
                    var result = await _emailService.SendCertificateExpiryNotificationAsync(
                        recipientEmail, 
                        certificateDetails);
                    
                    results.Add(result);
                }

                _logger.LogInformation("Sent {Count} expiry notifications", results.Count);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiry notifications");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("expiring-certificates")]
        public async Task<ActionResult<List<CertificateDto>>> GetExpiringCertificates([FromQuery] int days = 30)
        {
            try
            {
                var certificates = await _certificateRepository.GetExpiringCertificatesAsync(days);
                
                var certificateDtos = certificates.Select(c => new CertificateDto
                {
                    Id = c.Id,
                    SerialNumber = c.SerialNumber,
                    PersonName = c.PersonName,
                    ServiceMethod = c.ServiceMethod,
                    CertificateType = c.CertificateType,
                    ExpiryDate = c.ExpiryDate,
                    Country = c.Country,
                    State = c.State,
                    StreetAddress = c.StreetAddress,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsExpired = c.IsExpired
                }).ToList();

                return Ok(certificateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiring certificates");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
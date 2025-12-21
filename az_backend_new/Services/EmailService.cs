using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using az_backend_new.DTOs;

namespace az_backend_new.Services
{
    public interface IEmailService
    {
        Task<EmailResponseDto> SendEmailAsync(SendEmailDto emailDto);
        Task<EmailResponseDto> SendCertificateExpiryNotificationAsync(string recipientEmail, string certificateDetails);
        Task<EmailResponseDto> SendCertificateConfirmationAsync(string recipientEmail, string certificateDetails);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<EmailResponseDto> SendEmailAsync(SendEmailDto emailDto)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    emailSettings["SenderName"], 
                    emailSettings["SenderEmail"]
                ));
                message.To.Add(new MailboxAddress("", emailDto.To));
                message.Subject = emailDto.Subject;

                var bodyBuilder = new BodyBuilder();
                if (emailDto.IsHtml)
                {
                    bodyBuilder.HtmlBody = emailDto.Body;
                }
                else
                {
                    bodyBuilder.TextBody = emailDto.Body;
                }
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                
                // Connect with explicit SSL/TLS settings for Gmail
                await client.ConnectAsync(
                    emailSettings["SmtpServer"], 
                    int.Parse(emailSettings["Port"]!), 
                    SecureSocketOptions.StartTls
                );

                var username = emailSettings["Username"];
                var password = emailSettings["Password"];
                
                _logger.LogInformation("Attempting SMTP authentication for {Username}", username);
                
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await client.AuthenticateAsync(username, password);
                    _logger.LogInformation("SMTP authentication successful");
                }

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", emailDto.To);

                return new EmailResponseDto
                {
                    Success = true,
                    Message = "Email sent successfully",
                    SentAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", emailDto.To);
                
                return new EmailResponseDto
                {
                    Success = false,
                    Message = $"Failed to send email: {ex.Message}",
                    SentAt = DateTime.UtcNow
                };
            }
        }

        public async Task<EmailResponseDto> SendCertificateExpiryNotificationAsync(string recipientEmail, string certificateDetails)
        {
            var emailDto = new SendEmailDto
            {
                To = recipientEmail,
                Subject = "Certificate Expiry Notification - AZ International",
                Body = $@"
Dear Certificate Holder,

This is a notification that your certificate is expiring within 30 days.

Certificate Details:
{certificateDetails}

Please contact AZ International to renew your certificate before it expires.

Best regards,
AZ International Team
",
                IsHtml = false
            };

            return await SendEmailAsync(emailDto);
        }

        public async Task<EmailResponseDto> SendCertificateConfirmationAsync(string recipientEmail, string certificateDetails)
        {
            var emailDto = new SendEmailDto
            {
                To = recipientEmail,
                Subject = "Certificate Confirmation - AZ International",
                Body = $@"
Dear Certificate Holder,

Your certificate has been successfully created/updated.

Certificate Details:
{certificateDetails}

Thank you for choosing AZ International.

Best regards,
AZ International Team
",
                IsHtml = false
            };

            return await SendEmailAsync(emailDto);
        }
    }
}
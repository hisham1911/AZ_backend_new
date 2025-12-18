using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using az_backend_new.DTOs;
using az_backend_new.Models;
using az_backend_new.Repositories;
using az_backend_new.Services;
using ExcelDataReader;

namespace az_backend_new.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertificatesController : ControllerBase
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<CertificatesController> _logger;

        public CertificatesController(
            ICertificateRepository certificateRepository,
            IEmailService emailService,
            ILogger<CertificatesController> logger)
        {
            _certificateRepository = certificateRepository;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<CertificateDto>>> GetCertificates(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _certificateRepository.GetAllAsync(page, pageSize);
                
                var certificateDtos = result.Items.Select(MapToDto).ToList();
                
                return Ok(new PagedResult<CertificateDto>
                {
                    Items = certificateDtos,
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PageSize = result.PageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CertificateDto>> GetCertificate(int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                return Ok(MapToDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CertificateDto>> CreateCertificate(CreateCertificateDto createDto)
        {
            try
            {
                // Check if serial number already exists
                if (await _certificateRepository.SerialNumberExistsAsync(createDto.SerialNumber))
                {
                    return Conflict(new { message = "Serial number already exists" });
                }

                var certificate = new Certificate
                {
                    SerialNumber = createDto.SerialNumber,
                    PersonName = createDto.PersonName,
                    ServiceMethod = createDto.ServiceMethod,
                    CertificateType = createDto.CertificateType,
                    ExpiryDate = createDto.ExpiryDate,
                    Country = createDto.Country,
                    State = createDto.State,
                    StreetAddress = createDto.StreetAddress
                };

                certificate = await _certificateRepository.CreateAsync(certificate);

                _logger.LogInformation("Certificate created: {SerialNumber}", certificate.SerialNumber);

                return CreatedAtAction(
                    nameof(GetCertificate), 
                    new { id = certificate.Id }, 
                    MapToDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating certificate");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CertificateDto>> UpdateCertificate(int id, UpdateCertificateDto updateDto)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                // Check serial number uniqueness if it's being updated
                if (!string.IsNullOrEmpty(updateDto.SerialNumber) && 
                    updateDto.SerialNumber != certificate.SerialNumber)
                {
                    if (await _certificateRepository.SerialNumberExistsAsync(updateDto.SerialNumber, id))
                    {
                        return Conflict(new { message = "Serial number already exists" });
                    }
                    certificate.SerialNumber = updateDto.SerialNumber;
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(updateDto.PersonName))
                    certificate.PersonName = updateDto.PersonName;
                
                if (updateDto.ServiceMethod.HasValue)
                    certificate.ServiceMethod = updateDto.ServiceMethod.Value;
                
                if (updateDto.CertificateType.HasValue)
                    certificate.CertificateType = updateDto.CertificateType.Value;
                
                if (updateDto.ExpiryDate.HasValue)
                    certificate.ExpiryDate = updateDto.ExpiryDate.Value;
                
                if (updateDto.Country != null)
                    certificate.Country = updateDto.Country;
                
                if (updateDto.State != null)
                    certificate.State = updateDto.State;
                
                if (updateDto.StreetAddress != null)
                    certificate.StreetAddress = updateDto.StreetAddress;

                certificate = await _certificateRepository.UpdateAsync(certificate);

                _logger.LogInformation("Certificate updated: {SerialNumber}", certificate.SerialNumber);

                return Ok(MapToDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            try
            {
                var success = await _certificateRepository.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                _logger.LogInformation("Certificate deleted: {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting certificate {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<CertificateDto>>> SearchCertificates([FromQuery] CertificateSearchDto searchDto)
        {
            try
            {
                var certificates = await _certificateRepository.SearchAsync(searchDto);
                var certificateDtos = certificates.Select(MapToDto).ToList();

                return Ok(certificateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching certificates");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private static CertificateDto MapToDto(Certificate certificate)
        {
            return new CertificateDto
            {
                Id = certificate.Id,
                SerialNumber = certificate.SerialNumber,
                PersonName = certificate.PersonName,
                ServiceMethod = certificate.ServiceMethod,
                CertificateType = certificate.CertificateType,
                ExpiryDate = certificate.ExpiryDate,
                Country = certificate.Country,
                State = certificate.State,
                StreetAddress = certificate.StreetAddress,
                CreatedAt = certificate.CreatedAt,
                UpdatedAt = certificate.UpdatedAt,
                IsExpired = certificate.IsExpired
            };
        }
        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ImportResultDto>> ImportFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "No file uploaded" });
                }

                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Invalid file format. Only .xlsx and .xls files are allowed" });
                }

                var result = new ImportResultDto();
                var errors = new List<string>();

                using var stream = file.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateReader(stream);

                var rowIndex = 0;
                var certificatesToAdd = new List<Certificate>();

                while (reader.Read())
                {
                    rowIndex++;
                    
                    // Skip header row
                    if (rowIndex == 1) continue;

                    try
                    {
                        // Check if row has minimum required data
                        if (reader.FieldCount < 5)
                        {
                            errors.Add($"Row {rowIndex}: Insufficient columns");
                            continue;
                        }

                        var serialNumber = reader.GetValue(0)?.ToString()?.Trim();
                        var personName = reader.GetValue(1)?.ToString()?.Trim();
                        var serviceMethodStr = reader.GetValue(2)?.ToString()?.Trim();
                        var certificateTypeStr = reader.GetValue(3)?.ToString()?.Trim();
                        var expiryDateStr = reader.GetValue(4)?.ToString()?.Trim();

                        // Validate required fields
                        if (string.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(personName))
                        {
                            errors.Add($"Row {rowIndex}: Serial Number and Person Name are required");
                            continue;
                        }

                        // Check for duplicate serial number
                        if (await _certificateRepository.SerialNumberExistsAsync(serialNumber) ||
                            certificatesToAdd.Any(c => c.SerialNumber == serialNumber))
                        {
                            errors.Add($"Row {rowIndex}: Serial Number '{serialNumber}' already exists");
                            continue;
                        }

                        // Parse ServiceMethod
                        if (!Enum.TryParse<ServiceMethod>(serviceMethodStr, true, out var serviceMethod))
                        {
                            errors.Add($"Row {rowIndex}: Invalid Service Method '{serviceMethodStr}'");
                            continue;
                        }

                        // Parse CertificateType
                        if (!Enum.TryParse<CertificateType>(certificateTypeStr, true, out var certificateType))
                        {
                            errors.Add($"Row {rowIndex}: Invalid Certificate Type '{certificateTypeStr}'");
                            continue;
                        }

                        // Parse ExpiryDate
                        if (!DateTime.TryParse(expiryDateStr, out var expiryDate))
                        {
                            errors.Add($"Row {rowIndex}: Invalid Expiry Date '{expiryDateStr}'");
                            continue;
                        }

                        // Optional fields
                        var country = reader.FieldCount > 5 ? reader.GetValue(5)?.ToString()?.Trim() : null;
                        var state = reader.FieldCount > 6 ? reader.GetValue(6)?.ToString()?.Trim() : null;
                        var streetAddress = reader.FieldCount > 7 ? reader.GetValue(7)?.ToString()?.Trim() : null;

                        var certificate = new Certificate
                        {
                            SerialNumber = serialNumber,
                            PersonName = personName,
                            ServiceMethod = serviceMethod,
                            CertificateType = certificateType,
                            ExpiryDate = expiryDate,
                            Country = country,
                            State = state,
                            StreetAddress = streetAddress
                        };

                        certificatesToAdd.Add(certificate);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {rowIndex}: {ex.Message}");
                    }

                    result.TotalProcessed++;
                }

                // Save valid certificates
                foreach (var certificate in certificatesToAdd)
                {
                    try
                    {
                        await _certificateRepository.CreateAsync(certificate);
                        result.SuccessfulImports++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Failed to save certificate {certificate.SerialNumber}: {ex.Message}");
                        result.ErrorCount++;
                    }
                }

                result.ErrorCount = errors.Count;
                result.Errors = errors;

                _logger.LogInformation("Excel import completed: {Successful}/{Total} certificates imported", 
                    result.SuccessfulImports, result.TotalProcessed);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Excel file");
                return StatusCode(500, new { message = "Internal server error during import" });
            }
        }
    }
}
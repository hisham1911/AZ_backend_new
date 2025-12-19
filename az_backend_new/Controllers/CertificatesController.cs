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
                using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);

                var rowIndex = 0;

                // Method columns mapping: VT=2,3 | PT=4,5 | MT=6,7 | RT=8,9 | UT=10,11
                var methodColumns = new List<(ServiceMethod method, int typeCol, int dateCol, string code)>
                {
                    (ServiceMethod.VisualTesting, 2, 3, "VT"),
                    (ServiceMethod.LiquidPenetrantTesting, 4, 5, "PT"),
                    (ServiceMethod.MagneticParticleTesting, 6, 7, "MT"),
                    (ServiceMethod.RadiographicTesting, 8, 9, "RT"),
                    (ServiceMethod.UltrasonicTesting, 10, 11, "UT")
                };

                while (reader.Read())
                {
                    rowIndex++;
                    if (rowIndex <= 2) continue; // Skip header rows

                    try
                    {
                        var serialNumber = reader.GetValue(0)?.ToString()?.Trim();
                        var personName = reader.GetValue(1)?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(personName))
                            continue;

                        _logger.LogInformation("Processing row {Row}: S/N={SN}, Name={Name}", rowIndex, serialNumber, personName);

                        // Process each method column - create separate certificate for each
                        foreach (var (method, typeCol, dateCol, methodCode) in methodColumns)
                        {
                            try
                            {
                                if (reader.FieldCount <= dateCol) continue;

                                var certTypeStr = reader.GetValue(typeCol)?.ToString()?.Trim();
                                var expiryDateStr = reader.GetValue(dateCol)?.ToString()?.Trim();

                                // Skip if no data for this method
                                if (string.IsNullOrEmpty(certTypeStr) && string.IsNullOrEmpty(expiryDateStr))
                                    continue;

                                // Parse expiry date
                                if (!TryParseExcelDate(expiryDateStr, reader.GetValue(dateCol), out var expiryDate))
                                {
                                    errors.Add($"Row {rowIndex}, Method {methodCode}: Could not parse date '{expiryDateStr}'");
                                    continue;
                                }

                                var certType = ParseCertificateType(certTypeStr);
                                
                                // Create unique serial number: originalSN-MethodCode
                                var uniqueSerialNumber = $"{serialNumber}-{methodCode}";

                                // Check if certificate already exists
                                var existingCert = await _certificateRepository.GetBySerialNumberAsync(uniqueSerialNumber);
                                
                                if (existingCert != null)
                                {
                                    // Update existing certificate
                                    existingCert.PersonName = personName;
                                    existingCert.CertificateType = certType;
                                    existingCert.ExpiryDate = DateTime.SpecifyKind(expiryDate, DateTimeKind.Utc);
                                    existingCert.UpdatedAt = DateTime.UtcNow;
                                    
                                    await _certificateRepository.UpdateAsync(existingCert);
                                    result.UpdatedCount++;
                                }
                                else
                                {
                                    // Create new certificate
                                    var certificate = new Certificate
                                    {
                                        SerialNumber = uniqueSerialNumber,
                                        PersonName = personName,
                                        ServiceMethod = method,
                                        CertificateType = certType,
                                        ExpiryDate = DateTime.SpecifyKind(expiryDate, DateTimeKind.Utc),
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow
                                    };

                                    await _certificateRepository.CreateAsync(certificate);
                                    result.SuccessfulImports++;
                                }
                            }
                            catch (Exception methodEx)
                            {
                                errors.Add($"Row {rowIndex}, Method {methodCode}: {methodEx.Message}");
                                result.ErrorCount++;
                            }
                        }
                    }
                    catch (Exception rowEx)
                    {
                        errors.Add($"Row {rowIndex}: {rowEx.Message}");
                        result.ErrorCount++;
                    }

                    result.TotalProcessed++;
                }

                result.Errors = errors;

                _logger.LogInformation("Excel import completed: {Added} added, {Updated} updated, {Errors} errors", 
                    result.SuccessfulImports, result.UpdatedCount, result.ErrorCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Excel file");
                return StatusCode(500, new { message = "Internal server error during import" });
            }
        }

        private static CertificateType ParseCertificateType(string? typeStr)
        {
            if (string.IsNullOrEmpty(typeStr)) return CertificateType.Initial;
            
            var lower = typeStr.ToLower().Trim();
            if (lower.Contains("recert") || lower.Contains("renewal") || lower.Contains("re-cert"))
                return CertificateType.Recertificate;
            if (lower.Contains("initial") || lower.Contains("new"))
                return CertificateType.Initial;
            
            return CertificateType.Recertificate; // Default based on your data
        }

        private static bool TryParseExcelDate(string? dateStr, object? rawValue, out DateTime result)
        {
            result = DateTime.MinValue;
            
            // Try parsing raw value as DateTime (Excel stores dates as numbers)
            if (rawValue is DateTime dt)
            {
                result = dt;
                return true;
            }
            
            // Try parsing raw value as double (Excel date serial number)
            if (rawValue is double d)
            {
                try
                {
                    result = DateTime.FromOADate(d);
                    return true;
                }
                catch { }
            }

            if (string.IsNullOrEmpty(dateStr)) return false;

            // Try various date formats
            var formats = new[] { 
                "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy",
                "MM/dd/yyyy", "M/dd/yyyy", "MM/d/yyyy", "M/d/yyyy",
                "yyyy-MM-dd", "yyyy/MM/dd",
                "dd-MM-yyyy", "d-MM-yyyy"
            };

            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out result))
                    return true;
            }

            // Try general parse
            return DateTime.TryParse(dateStr, out result);
        }
    }
}
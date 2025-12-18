using Microsoft.AspNetCore.Mvc;
using az_backend_new.DTOs;
using az_backend_new.Models;
using az_backend_new.Repositories;
using ExcelDataReader;

namespace az_backend_new.Controllers
{
    /// <summary>
    /// Legacy API controller for backward compatibility with existing frontend
    /// Maps old /api/Services endpoints to new certificate functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ServicesController : ControllerBase
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(
            ICertificateRepository certificateRepository,
            ILogger<ServicesController> logger)
        {
            _certificateRepository = certificateRepository;
            _logger = logger;
        }

        /// <summary>
        /// Get all certificates (legacy format)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<LegacyCertificateDto>>> GetAllServices()
        {
            try
            {
                var result = await _certificateRepository.GetAllAsync(1, 1000);
                var legacyDtos = result.Items.Select(MapToLegacyDto).ToList();
                return Ok(legacyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all services");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get certificate by ID (legacy format)
        /// </summary>
        [HttpGet("getById")]
        public async Task<ActionResult<LegacyCertificateDto>> GetById([FromQuery] int id)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound(new { message = "Certificate not found" });
                }
                return Ok(MapToLegacyDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service by ID {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Search by name (legacy format)
        /// </summary>
        [HttpGet("searchByName")]
        public async Task<ActionResult<List<LegacyCertificateDto>>> SearchByName([FromQuery] string search)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    return Ok(new List<LegacyCertificateDto>());
                }

                var searchDto = new CertificateSearchDto { PersonName = search };
                var certificates = await _certificateRepository.SearchAsync(searchDto);
                
                if (!certificates.Any())
                {
                    return NotFound(new { message = "No certificates found" });
                }

                var legacyDtos = certificates.Select(MapToLegacyDto).ToList();
                return Ok(legacyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching by name");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Search by serial number (legacy format)
        /// </summary>
        [HttpGet("searchByS_N")]
        public async Task<ActionResult<List<LegacyCertificateDto>>> SearchBySerialNumber([FromQuery] string search)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    return Ok(new List<LegacyCertificateDto>());
                }

                var searchDto = new CertificateSearchDto { SerialNumber = search };
                var certificates = await _certificateRepository.SearchAsync(searchDto);
                
                if (!certificates.Any())
                {
                    return NotFound(new { message = "No certificates found" });
                }

                var legacyDtos = certificates.Select(MapToLegacyDto).ToList();
                return Ok(legacyDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching by serial number");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create certificate (legacy format)
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<LegacyCertificateDto>> CreateService([FromBody] LegacyCreateDto createDto)
        {
            try
            {
                _logger.LogInformation("Creating certificate: Name={Name}, S_N={S_N}, Method={Method}, Type={Type}, EndDate={EndDate}", 
                    createDto.Name, createDto.S_N, createDto.Method, createDto.Type, createDto.EndDate);

                // Validate required fields
                if (string.IsNullOrWhiteSpace(createDto.Name) || string.IsNullOrWhiteSpace(createDto.S_N))
                {
                    return BadRequest(new { message = "Name and Serial Number are required" });
                }

                // Check if serial number already exists
                if (await _certificateRepository.SerialNumberExistsAsync(createDto.S_N))
                {
                    return Conflict(new { message = "Serial number already exists" });
                }

                // Parse expiry date safely
                DateTime expiryDate;
                if (!string.IsNullOrWhiteSpace(createDto.EndDate))
                {
                    if (!DateTime.TryParse(createDto.EndDate, out expiryDate))
                    {
                        expiryDate = DateTime.UtcNow.AddYears(2);
                    }
                }
                else
                {
                    expiryDate = DateTime.UtcNow.AddYears(2);
                }

                var certificate = new Certificate
                {
                    SerialNumber = createDto.S_N,
                    PersonName = createDto.Name,
                    ServiceMethod = createDto.Method > 0 ? (ServiceMethod)createDto.Method : ServiceMethod.MagneticParticleTesting,
                    CertificateType = createDto.Type > 0 ? (CertificateType)createDto.Type : CertificateType.Initial,
                    ExpiryDate = expiryDate,
                    Country = createDto.Country ?? "",
                    State = createDto.State ?? "",
                    StreetAddress = createDto.StreetAddress ?? ""
                };

                certificate = await _certificateRepository.CreateAsync(certificate);
                _logger.LogInformation("Certificate created via legacy API: {SerialNumber}", certificate.SerialNumber);

                return Ok(MapToLegacyDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Update certificate (legacy format)
        /// </summary>
        [HttpPut("update/{id}")]
        public async Task<ActionResult<LegacyCertificateDto>> UpdateService(int id, [FromBody] LegacyUpdateDto updateDto)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                // Check serial number uniqueness if it's being updated
                if (!string.IsNullOrEmpty(updateDto.S_N) && updateDto.S_N != certificate.SerialNumber)
                {
                    if (await _certificateRepository.SerialNumberExistsAsync(updateDto.S_N, id))
                    {
                        return Conflict(new { message = "Serial number already exists" });
                    }
                    certificate.SerialNumber = updateDto.S_N;
                }

                if (!string.IsNullOrEmpty(updateDto.Name))
                    certificate.PersonName = updateDto.Name;
                
                if (updateDto.Method > 0)
                    certificate.ServiceMethod = (ServiceMethod)updateDto.Method;
                
                if (updateDto.Type > 0)
                    certificate.CertificateType = (CertificateType)updateDto.Type;
                
                if (!string.IsNullOrEmpty(updateDto.EndDate))
                    certificate.ExpiryDate = DateTime.Parse(updateDto.EndDate);

                certificate = await _certificateRepository.UpdateAsync(certificate);
                _logger.LogInformation("Certificate updated via legacy API: {SerialNumber}", certificate.SerialNumber);

                return Ok(MapToLegacyDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete certificate (legacy format)
        /// </summary>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteService(int id)
        {
            try
            {
                var success = await _certificateRepository.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                _logger.LogInformation("Certificate deleted via legacy API: {Id}", id);
                return Ok(new { message = "Certificate deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload Excel file (legacy format)
        /// </summary>
        [HttpPost("UploadExcelFile")]
        public async Task<ActionResult> UploadExcelFile(IFormFile file)
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
                    return BadRequest(new { message = "Invalid file format" });
                }

                var addedCount = 0;
                var errors = new List<string>();

                using var stream = file.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateReader(stream);

                var rowIndex = 0;

                while (reader.Read())
                {
                    rowIndex++;
                    if (rowIndex == 1) continue; // Skip header

                    try
                    {
                        if (reader.FieldCount < 5) continue;

                        var serialNumber = reader.GetValue(0)?.ToString()?.Trim();
                        var personName = reader.GetValue(1)?.ToString()?.Trim();
                        var serviceMethodStr = reader.GetValue(2)?.ToString()?.Trim();
                        var certificateTypeStr = reader.GetValue(3)?.ToString()?.Trim();
                        var expiryDateStr = reader.GetValue(4)?.ToString()?.Trim();

                        if (string.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(personName))
                            continue;

                        if (await _certificateRepository.SerialNumberExistsAsync(serialNumber))
                            continue;

                        // Try to parse method and type as numbers first
                        int methodNum = 1, typeNum = 1;
                        if (!int.TryParse(serviceMethodStr, out methodNum))
                        {
                            if (!Enum.TryParse<ServiceMethod>(serviceMethodStr, true, out var sm))
                                continue;
                            methodNum = (int)sm;
                        }

                        if (!int.TryParse(certificateTypeStr, out typeNum))
                        {
                            if (!Enum.TryParse<CertificateType>(certificateTypeStr, true, out var ct))
                                continue;
                            typeNum = (int)ct;
                        }

                        if (!DateTime.TryParse(expiryDateStr, out var expiryDate))
                            continue;

                        var certificate = new Certificate
                        {
                            SerialNumber = serialNumber,
                            PersonName = personName,
                            ServiceMethod = (ServiceMethod)methodNum,
                            CertificateType = (CertificateType)typeNum,
                            ExpiryDate = expiryDate
                        };

                        await _certificateRepository.CreateAsync(certificate);
                        addedCount++;
                    }
                    catch { }
                }

                return Ok(new { success = true, message = $"Successfully uploaded {addedCount} certificates", addedCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading Excel file");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private static LegacyCertificateDto MapToLegacyDto(Certificate certificate)
        {
            return new LegacyCertificateDto
            {
                SrId = certificate.Id,
                Name = certificate.PersonName,
                S_N = certificate.SerialNumber,
                Method = (int)certificate.ServiceMethod,
                Type = (int)certificate.CertificateType,
                EndDate = certificate.ExpiryDate.ToString("yyyy-MM-dd"),
                Country = certificate.Country ?? "",
                State = certificate.State ?? "",
                StreetAddress = certificate.StreetAddress ?? ""
            };
        }
    }

    // Legacy DTOs for backward compatibility
    public class LegacyCertificateDto
    {
        public int SrId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string S_N { get; set; } = string.Empty;
        public int Method { get; set; }
        public int Type { get; set; }
        public string EndDate { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
    }

    public class LegacyCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string S_N { get; set; } = string.Empty;
        public int Method { get; set; }
        public int Type { get; set; }
        public string EndDate { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? StreetAddress { get; set; }
    }

    public class LegacyUpdateDto
    {
        public int SrId { get; set; }
        public string? Name { get; set; }
        public string? S_N { get; set; }
        public int Method { get; set; }
        public int Type { get; set; }
        public string? EndDate { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? StreetAddress { get; set; }
    }
}

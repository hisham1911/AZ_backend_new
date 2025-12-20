using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using az_backend_new.DTOs;
using az_backend_new.Models;
using az_backend_new.Repositories;
using ExcelDataReader;

namespace az_backend_new.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TraineesController : ControllerBase
    {
        private readonly ITraineeRepository _traineeRepository;
        private readonly ILogger<TraineesController> _logger;

        public TraineesController(
            ITraineeRepository traineeRepository,
            ILogger<TraineesController> logger)
        {
            _traineeRepository = traineeRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<TraineeDto>>> GetTrainees(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _traineeRepository.GetAllWithCertificatesAsync(page, pageSize);
                var traineeDtos = result.Items.Select(MapToDto).ToList();
                
                return Ok(new PagedResult<TraineeDto>
                {
                    Items = traineeDtos,
                    TotalCount = result.TotalCount,
                    Page = result.Page,
                    PageSize = result.PageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trainees");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TraineeDto>> GetTrainee(int id)
        {
            try
            {
                var trainee = await _traineeRepository.GetByIdWithCertificatesAsync(id);
                if (trainee == null)
                    return NotFound(new { message = "Trainee not found" });

                return Ok(MapToDto(trainee));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trainee {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<TraineeDto>>> SearchTrainees([FromQuery] TraineeSearchDto searchDto)
        {
            try
            {
                var trainees = await _traineeRepository.SearchAsync(searchDto);
                return Ok(trainees.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching trainees");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TraineeDto>> CreateTrainee(CreateTraineeDto createDto)
        {
            try
            {
                if (await _traineeRepository.SerialNumberExistsAsync(createDto.SerialNumber))
                    return Conflict(new { message = "Serial number already exists" });

                var methods = createDto.Certificates.Select(c => c.ServiceMethod).ToList();
                if (methods.Count != methods.Distinct().Count())
                    return BadRequest(new { message = "Cannot have duplicate service methods" });

                var trainee = new Trainee
                {
                    SerialNumber = createDto.SerialNumber,
                    PersonName = createDto.PersonName,
                    Country = createDto.Country,
                    State = createDto.State,
                    StreetAddress = createDto.StreetAddress,
                    Certificates = createDto.Certificates.Select(c => new Certificate
                    {
                        ServiceMethod = c.ServiceMethod,
                        CertificateType = c.CertificateType,
                        ExpiryDate = DateTime.SpecifyKind(c.ExpiryDate, DateTimeKind.Utc),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList()
                };

                trainee = await _traineeRepository.CreateAsync(trainee);
                trainee = await _traineeRepository.GetByIdWithCertificatesAsync(trainee.Id);

                _logger.LogInformation("Trainee created: {SerialNumber}", trainee!.SerialNumber);

                return CreatedAtAction(nameof(GetTrainee), new { id = trainee.Id }, MapToDto(trainee));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trainee");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TraineeDto>> UpdateTrainee(int id, UpdateTraineeDto updateDto)
        {
            try
            {
                var trainee = await _traineeRepository.GetByIdWithCertificatesAsync(id);
                if (trainee == null)
                    return NotFound(new { message = "Trainee not found" });

                if (!string.IsNullOrEmpty(updateDto.SerialNumber) && updateDto.SerialNumber != trainee.SerialNumber)
                {
                    if (await _traineeRepository.SerialNumberExistsAsync(updateDto.SerialNumber, id))
                        return Conflict(new { message = "Serial number already exists" });
                    trainee.SerialNumber = updateDto.SerialNumber;
                }

                if (!string.IsNullOrEmpty(updateDto.PersonName))
                    trainee.PersonName = updateDto.PersonName;
                if (updateDto.Country != null)
                    trainee.Country = updateDto.Country;
                if (updateDto.State != null)
                    trainee.State = updateDto.State;
                if (updateDto.StreetAddress != null)
                    trainee.StreetAddress = updateDto.StreetAddress;

                trainee = await _traineeRepository.UpdateAsync(trainee);
                _logger.LogInformation("Trainee updated: {SerialNumber}", trainee.SerialNumber);

                return Ok(MapToDto(trainee));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trainee {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTrainee(int id)
        {
            try
            {
                if (!await _traineeRepository.DeleteAsync(id))
                    return NotFound(new { message = "Trainee not found" });

                _logger.LogInformation("Trainee deleted: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting trainee {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("{traineeId}/certificates")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TraineeCertificateDto>> AddCertificate(int traineeId, AddCertificateToTraineeDto addDto)
        {
            try
            {
                var trainee = await _traineeRepository.GetByIdAsync(traineeId);
                if (trainee == null)
                    return NotFound(new { message = "Trainee not found" });

                if (await _traineeRepository.TraineeHasCertificateWithMethodAsync(traineeId, addDto.ServiceMethod))
                    return Conflict(new { message = $"Trainee already has a certificate for {addDto.ServiceMethod}" });

                var certificate = new Certificate
                {
                    ServiceMethod = addDto.ServiceMethod,
                    CertificateType = addDto.CertificateType,
                    ExpiryDate = DateTime.SpecifyKind(addDto.ExpiryDate, DateTimeKind.Utc)
                };

                certificate = await _traineeRepository.AddCertificateAsync(traineeId, certificate);
                _logger.LogInformation("Certificate added to trainee {TraineeId}: {Method}", traineeId, certificate.ServiceMethod);

                return CreatedAtAction(nameof(GetCertificate), new { traineeId, certificateId = certificate.Id }, MapCertificateToDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding certificate to trainee {TraineeId}", traineeId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{traineeId}/certificates/{certificateId}")]
        public async Task<ActionResult<TraineeCertificateDto>> GetCertificate(int traineeId, int certificateId)
        {
            try
            {
                var certificate = await _traineeRepository.GetCertificateByIdAsync(certificateId);
                if (certificate == null || certificate.TraineeId != traineeId)
                    return NotFound(new { message = "Certificate not found" });

                return Ok(MapCertificateToDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate {CertificateId}", certificateId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{traineeId}/certificates/{certificateId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TraineeCertificateDto>> UpdateCertificate(int traineeId, int certificateId, UpdateTraineeCertificateDto updateDto)
        {
            try
            {
                var certificate = await _traineeRepository.GetCertificateByIdAsync(certificateId);
                if (certificate == null || certificate.TraineeId != traineeId)
                    return NotFound(new { message = "Certificate not found" });

                if (updateDto.ServiceMethod.HasValue && updateDto.ServiceMethod.Value != certificate.ServiceMethod)
                {
                    if (await _traineeRepository.TraineeHasCertificateWithMethodAsync(traineeId, updateDto.ServiceMethod.Value, certificateId))
                        return Conflict(new { message = $"Trainee already has a certificate for {updateDto.ServiceMethod.Value}" });
                    certificate.ServiceMethod = updateDto.ServiceMethod.Value;
                }

                if (updateDto.CertificateType.HasValue)
                    certificate.CertificateType = updateDto.CertificateType.Value;
                if (updateDto.ExpiryDate.HasValue)
                    certificate.ExpiryDate = DateTime.SpecifyKind(updateDto.ExpiryDate.Value, DateTimeKind.Utc);

                certificate = await _traineeRepository.UpdateCertificateAsync(certificate);
                _logger.LogInformation("Certificate updated: {CertificateId}", certificateId);

                return Ok(MapCertificateToDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate {CertificateId}", certificateId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{traineeId}/certificates/{certificateId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCertificate(int traineeId, int certificateId)
        {
            try
            {
                var certificate = await _traineeRepository.GetCertificateByIdAsync(certificateId);
                if (certificate == null || certificate.TraineeId != traineeId)
                    return NotFound(new { message = "Certificate not found" });

                if (!await _traineeRepository.DeleteCertificateAsync(certificateId))
                    return NotFound(new { message = "Certificate not found" });

                _logger.LogInformation("Certificate deleted: {CertificateId}", certificateId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting certificate {CertificateId}", certificateId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            try
            {
                var allTrainees = await _traineeRepository.GetAllWithCertificatesAsync(1, int.MaxValue);
                var trainees = allTrainees.Items;
                var allCertificates = trainees.SelectMany(t => t.Certificates).ToList();

                return Ok(new
                {
                    totalTrainees = trainees.Count,
                    totalCertificates = allCertificates.Count,
                    expiredCertificates = allCertificates.Count(c => c.IsExpired),
                    activeCertificates = allCertificates.Count(c => !c.IsExpired),
                    byServiceMethod = allCertificates.GroupBy(c => c.ServiceMethod).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    byCertificateType = allCertificates.GroupBy(c => c.CertificateType).ToDictionary(g => g.Key.ToString(), g => g.Count())
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// حذف جميع البيانات
        /// </summary>
        [HttpDelete("delete-all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _traineeRepository.DeleteAllAsync();
                _logger.LogInformation("All trainees and certificates deleted");
                return Ok(new { message = "All data deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all data");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// رفع ملف إكسيل لاستيراد المتدربين والشهادات
        /// </summary>
        [HttpPost("import")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { message = "Invalid file format. Please upload an Excel file (.xlsx or .xls)" });

            try
            {
                var importedCount = 0;
                var errors = new List<string>();

                using (var stream = file.OpenReadStream())
                {
                    using var reader = ExcelReaderFactory.CreateReader(stream);
                    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = true }
                    });

                    if (dataSet.Tables.Count == 0)
                        return BadRequest(new { message = "Excel file is empty" });

                    var table = dataSet.Tables[0];

                    for (int row = 0; row < table.Rows.Count; row++)
                    {
                        try
                        {
                            var dataRow = table.Rows[row];
                            
                            // قراءة البيانات من الصف
                            var serialNumber = dataRow[0]?.ToString()?.Trim() ?? "";
                            var personName = dataRow[1]?.ToString()?.Trim() ?? "";
                            var methodStr = dataRow[2]?.ToString()?.Trim() ?? "1";
                            var typeStr = dataRow[3]?.ToString()?.Trim() ?? "1";
                            var expiryDateStr = dataRow[4]?.ToString()?.Trim() ?? "";

                            if (string.IsNullOrEmpty(serialNumber) || string.IsNullOrEmpty(personName))
                            {
                                errors.Add($"Row {row + 2}: Missing serial number or person name");
                                continue;
                            }

                            // تحويل الطريقة
                            var serviceMethod = ParseServiceMethod(methodStr);
                            var certificateType = ParseCertificateType(typeStr);
                            
                            // تحويل التاريخ
                            DateTime expiryDate;
                            if (!DateTime.TryParse(expiryDateStr, out expiryDate))
                            {
                                expiryDate = DateTime.UtcNow.AddYears(2);
                            }
                            expiryDate = DateTime.SpecifyKind(expiryDate, DateTimeKind.Utc);

                            // البحث عن المتدرب أو إنشاء جديد
                            var existingTrainee = await _traineeRepository.GetBySerialNumberWithCertificatesAsync(serialNumber);
                            
                            if (existingTrainee != null)
                            {
                                // إضافة شهادة جديدة إذا لم تكن موجودة
                                if (!await _traineeRepository.TraineeHasCertificateWithMethodAsync(existingTrainee.Id, serviceMethod))
                                {
                                    var cert = new Certificate
                                    {
                                        ServiceMethod = serviceMethod,
                                        CertificateType = certificateType,
                                        ExpiryDate = expiryDate,
                                        CreatedAt = DateTime.UtcNow,
                                        UpdatedAt = DateTime.UtcNow
                                    };
                                    await _traineeRepository.AddCertificateAsync(existingTrainee.Id, cert);
                                    importedCount++;
                                }
                            }
                            else
                            {
                                // إنشاء متدرب جديد مع الشهادة
                                var trainee = new Trainee
                                {
                                    SerialNumber = serialNumber,
                                    PersonName = personName,
                                    Certificates = new List<Certificate>
                                    {
                                        new Certificate
                                        {
                                            ServiceMethod = serviceMethod,
                                            CertificateType = certificateType,
                                            ExpiryDate = expiryDate,
                                            CreatedAt = DateTime.UtcNow,
                                            UpdatedAt = DateTime.UtcNow
                                        }
                                    }
                                };
                                await _traineeRepository.CreateAsync(trainee);
                                importedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Row {row + 2}: {ex.Message}");
                        }
                    }
                }

                _logger.LogInformation("Excel import completed: {Count} records imported", importedCount);

                return Ok(new
                {
                    message = $"Import completed successfully",
                    importedCount,
                    errors = errors.Take(10).ToList(),
                    totalErrors = errors.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Excel file");
                return StatusCode(500, new { message = "Error processing Excel file: " + ex.Message });
            }
        }

        private static ServiceMethod ParseServiceMethod(string value)
        {
            if (int.TryParse(value, out int num))
                return (ServiceMethod)num;
            
            return value.ToUpperInvariant() switch
            {
                "VT" or "VISUAL" or "VISUAL TESTING" => ServiceMethod.VisualTesting,
                "PT" or "PENETRANT" or "LIQUID PENETRANT TESTING" => ServiceMethod.LiquidPenetrantTesting,
                "MT" or "MAGNETIC" or "MAGNETIC PARTICLE TESTING" => ServiceMethod.MagneticParticleTesting,
                "RT" or "RADIOGRAPHIC" or "RADIOGRAPHIC TESTING" => ServiceMethod.RadiographicTesting,
                "UT" or "ULTRASONIC" or "ULTRASONIC TESTING" => ServiceMethod.UltrasonicTesting,
                _ => ServiceMethod.VisualTesting
            };
        }

        private static CertificateType ParseCertificateType(string value)
        {
            if (int.TryParse(value, out int num))
                return (CertificateType)num;
            
            return value.ToUpperInvariant() switch
            {
                "INITIAL" or "1" => CertificateType.Initial,
                "RECERTIFICATE" or "RECERT" or "2" => CertificateType.Recertificate,
                _ => CertificateType.Initial
            };
        }

        private static TraineeDto MapToDto(Trainee trainee)
        {
            return new TraineeDto
            {
                Id = trainee.Id,
                SerialNumber = trainee.SerialNumber,
                PersonName = trainee.PersonName,
                Country = trainee.Country,
                State = trainee.State,
                StreetAddress = trainee.StreetAddress,
                CreatedAt = trainee.CreatedAt,
                UpdatedAt = trainee.UpdatedAt,
                Certificates = trainee.Certificates.Select(MapCertificateToDto).ToList()
            };
        }

        private static TraineeCertificateDto MapCertificateToDto(Certificate certificate)
        {
            return new TraineeCertificateDto
            {
                Id = certificate.Id,
                ServiceMethod = certificate.ServiceMethod,
                MethodCode = certificate.MethodCode,
                CertificateType = certificate.CertificateType,
                ExpiryDate = certificate.ExpiryDate,
                IsExpired = certificate.IsExpired,
                CreatedAt = certificate.CreatedAt,
                UpdatedAt = certificate.UpdatedAt
            };
        }
    }
}

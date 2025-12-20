using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using az_backend_new.DTOs;
using az_backend_new.Models;
using az_backend_new.Repositories;
using az_backend_new.Data;
using Microsoft.EntityFrameworkCore;

namespace az_backend_new.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TraineesController : ControllerBase
    {
        private readonly ITraineeRepository _traineeRepository;
        private readonly AzDbContext _context;
        private readonly ILogger<TraineesController> _logger;

        public TraineesController(
            ITraineeRepository traineeRepository,
            AzDbContext context,
            ILogger<TraineesController> logger)
        {
            _traineeRepository = traineeRepository;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// الحصول على جميع المتدربين مع شهاداتهم
        /// </summary>
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

        /// <summary>
        /// الحصول على متدرب بالـ ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TraineeDto>> GetTrainee(int id)
        {
            try
            {
                var trainee = await _traineeRepository.GetByIdWithCertificatesAsync(id);
                if (trainee == null)
                {
                    return NotFound(new { message = "Trainee not found" });
                }

                return Ok(MapToDto(trainee));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trainee {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// البحث عن المتدربين
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<TraineeDto>>> SearchTrainees([FromQuery] TraineeSearchDto searchDto)
        {
            try
            {
                var trainees = await _traineeRepository.SearchAsync(searchDto);
                var traineeDtos = trainees.Select(MapToDto).ToList();

                return Ok(traineeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching trainees");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// إنشاء متدرب جديد مع شهاداته
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TraineeDto>> CreateTrainee(CreateTraineeDto createDto)
        {
            try
            {
                // التحقق من عدم وجود رقم تسلسلي مكرر
                if (await _traineeRepository.SerialNumberExistsAsync(createDto.SerialNumber))
                {
                    return Conflict(new { message = "Serial number already exists" });
                }

                // التحقق من عدم تكرار نفس الطريقة في الشهادات
                var methods = createDto.Certificates.Select(c => c.ServiceMethod).ToList();
                if (methods.Count != methods.Distinct().Count())
                {
                    return BadRequest(new { message = "Cannot have duplicate service methods for the same trainee" });
                }

                var trainee = new Trainee
                {
                    SerialNumber = createDto.SerialNumber,
                    PersonName = createDto.PersonName,
                    Country = createDto.Country,
                    State = createDto.State,
                    StreetAddress = createDto.StreetAddress,
                    Certificates = createDto.Certificates.Select(c => new CertificateNew
                    {
                        ServiceMethod = c.ServiceMethod,
                        CertificateType = c.CertificateType,
                        ExpiryDate = DateTime.SpecifyKind(c.ExpiryDate, DateTimeKind.Utc),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList()
                };

                trainee = await _traineeRepository.CreateAsync(trainee);

                // إعادة تحميل المتدرب مع الشهادات
                trainee = await _traineeRepository.GetByIdWithCertificatesAsync(trainee.Id);

                _logger.LogInformation("Trainee created: {SerialNumber}", trainee!.SerialNumber);

                return CreatedAtAction(
                    nameof(GetTrainee), 
                    new { id = trainee.Id }, 
                    MapToDto(trainee));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trainee");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// تحديث بيانات المتدرب
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TraineeDto>> UpdateTrainee(int id, UpdateTraineeDto updateDto)
        {
            try
            {
                var trainee = await _traineeRepository.GetByIdWithCertificatesAsync(id);
                if (trainee == null)
                {
                    return NotFound(new { message = "Trainee not found" });
                }

                // التحقق من الرقم التسلسلي إذا تم تغييره
                if (!string.IsNullOrEmpty(updateDto.SerialNumber) && 
                    updateDto.SerialNumber != trainee.SerialNumber)
                {
                    if (await _traineeRepository.SerialNumberExistsAsync(updateDto.SerialNumber, id))
                    {
                        return Conflict(new { message = "Serial number already exists" });
                    }
                    trainee.SerialNumber = updateDto.SerialNumber;
                }

                // تحديث الحقول المقدمة فقط
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

        /// <summary>
        /// حذف متدرب وجميع شهاداته
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTrainee(int id)
        {
            try
            {
                var success = await _traineeRepository.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Trainee not found" });
                }

                _logger.LogInformation("Trainee deleted: {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting trainee {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// إضافة شهادة لمتدرب موجود
        /// </summary>
        [HttpPost("{traineeId}/certificates")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TraineeCertificateDto>> AddCertificate(int traineeId, AddCertificateToTraineeDto addDto)
        {
            try
            {
                var trainee = await _traineeRepository.GetByIdAsync(traineeId);
                if (trainee == null)
                {
                    return NotFound(new { message = "Trainee not found" });
                }

                // التحقق من عدم وجود شهادة بنفس الطريقة
                if (await _traineeRepository.TraineeHasCertificateWithMethodAsync(traineeId, addDto.ServiceMethod))
                {
                    return Conflict(new { message = $"Trainee already has a certificate for {addDto.ServiceMethod}" });
                }

                var certificate = new CertificateNew
                {
                    ServiceMethod = addDto.ServiceMethod,
                    CertificateType = addDto.CertificateType,
                    ExpiryDate = DateTime.SpecifyKind(addDto.ExpiryDate, DateTimeKind.Utc)
                };

                certificate = await _traineeRepository.AddCertificateAsync(traineeId, certificate);

                _logger.LogInformation("Certificate added to trainee {TraineeId}: {Method}", traineeId, certificate.ServiceMethod);

                return CreatedAtAction(
                    nameof(GetCertificate), 
                    new { traineeId, certificateId = certificate.Id }, 
                    MapCertificateToDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding certificate to trainee {TraineeId}", traineeId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// الحصول على شهادة محددة
        /// </summary>
        [HttpGet("{traineeId}/certificates/{certificateId}")]
        public async Task<ActionResult<TraineeCertificateDto>> GetCertificate(int traineeId, int certificateId)
        {
            try
            {
                var certificate = await _traineeRepository.GetCertificateByIdAsync(certificateId);
                if (certificate == null || certificate.TraineeId != traineeId)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                return Ok(MapCertificateToDto(certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate {CertificateId}", certificateId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// تحديث شهادة
        /// </summary>
        [HttpPut("{traineeId}/certificates/{certificateId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TraineeCertificateDto>> UpdateCertificate(
            int traineeId, 
            int certificateId, 
            UpdateTraineeCertificateDto updateDto)
        {
            try
            {
                var certificate = await _traineeRepository.GetCertificateByIdAsync(certificateId);
                if (certificate == null || certificate.TraineeId != traineeId)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                // التحقق من عدم تكرار الطريقة إذا تم تغييرها
                if (updateDto.ServiceMethod.HasValue && 
                    updateDto.ServiceMethod.Value != certificate.ServiceMethod)
                {
                    if (await _traineeRepository.TraineeHasCertificateWithMethodAsync(
                        traineeId, updateDto.ServiceMethod.Value, certificateId))
                    {
                        return Conflict(new { message = $"Trainee already has a certificate for {updateDto.ServiceMethod.Value}" });
                    }
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

        /// <summary>
        /// حذف شهادة
        /// </summary>
        [HttpDelete("{traineeId}/certificates/{certificateId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCertificate(int traineeId, int certificateId)
        {
            try
            {
                var certificate = await _traineeRepository.GetCertificateByIdAsync(certificateId);
                if (certificate == null || certificate.TraineeId != traineeId)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                var success = await _traineeRepository.DeleteCertificateAsync(certificateId);
                if (!success)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                _logger.LogInformation("Certificate deleted: {CertificateId}", certificateId);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting certificate {CertificateId}", certificateId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// الحصول على الإحصائيات
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetStats()
        {
            try
            {
                var allTrainees = await _traineeRepository.GetAllWithCertificatesAsync(1, int.MaxValue);
                var trainees = allTrainees.Items;
                var allCertificates = trainees.SelectMany(t => t.Certificates).ToList();

                var stats = new
                {
                    totalTrainees = trainees.Count,
                    totalCertificates = allCertificates.Count,
                    expiredCertificates = allCertificates.Count(c => c.IsExpired),
                    activeCertificates = allCertificates.Count(c => !c.IsExpired),
                    byServiceMethod = allCertificates
                        .GroupBy(c => c.ServiceMethod)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    byCertificateType = allCertificates
                        .GroupBy(c => c.CertificateType)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                    recentlyCreated = trainees.Count(t => t.CreatedAt > DateTime.UtcNow.AddDays(-30))
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// ترحيل البيانات من الجدول القديم إلى الهيكل الجديد
        /// </summary>
        [HttpPost("migrate-from-old")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> MigrateFromOldTable()
        {
            try
            {
                var oldCertificates = await _context.Certificates.ToListAsync();
                
                var migratedTrainees = 0;
                var migratedCertificates = 0;
                var skippedDuplicates = 0;
                var errors = new List<string>();

                // تجميع الشهادات حسب الرقم التسلسلي الأساسي
                var groupedBySerial = oldCertificates
                    .GroupBy(c => ExtractBaseSerialNumber(c.SerialNumber))
                    .ToList();

                foreach (var group in groupedBySerial)
                {
                    var baseSerialNumber = group.Key;
                    var firstCert = group.First();

                    try
                    {
                        // التحقق من وجود المتدرب
                        var existingTrainee = await _traineeRepository.GetBySerialNumberWithCertificatesAsync(baseSerialNumber);
                        
                        Trainee trainee;
                        if (existingTrainee == null)
                        {
                            // إنشاء متدرب جديد
                            trainee = new Trainee
                            {
                                SerialNumber = baseSerialNumber,
                                PersonName = firstCert.PersonName,
                                Country = firstCert.Country,
                                State = firstCert.State,
                                StreetAddress = firstCert.StreetAddress,
                                CreatedAt = firstCert.CreatedAt,
                                UpdatedAt = DateTime.UtcNow
                            };
                            trainee = await _traineeRepository.CreateAsync(trainee);
                            migratedTrainees++;
                        }
                        else
                        {
                            trainee = existingTrainee;
                        }

                        // إضافة الشهادات
                        foreach (var oldCert in group)
                        {
                            var method = oldCert.ServiceMethod;
                            
                            // التحقق من عدم وجود شهادة بنفس الطريقة
                            if (await _traineeRepository.TraineeHasCertificateWithMethodAsync(trainee.Id, method))
                            {
                                skippedDuplicates++;
                                continue;
                            }

                            var newCert = new CertificateNew
                            {
                                ServiceMethod = method,
                                CertificateType = oldCert.CertificateType,
                                ExpiryDate = DateTime.SpecifyKind(oldCert.ExpiryDate, DateTimeKind.Utc),
                                CreatedAt = oldCert.CreatedAt,
                                UpdatedAt = DateTime.UtcNow
                            };

                            await _traineeRepository.AddCertificateAsync(trainee.Id, newCert);
                            migratedCertificates++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error migrating serial {baseSerialNumber}: {ex.Message}");
                    }
                }

                _logger.LogInformation(
                    "Migration completed: {Trainees} trainees, {Certificates} certificates, {Skipped} skipped",
                    migratedTrainees, migratedCertificates, skippedDuplicates);

                return Ok(new
                {
                    message = "Migration completed",
                    migratedTrainees,
                    migratedCertificates,
                    skippedDuplicates,
                    totalOldCertificates = oldCertificates.Count,
                    errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during migration");
                return StatusCode(500, new { message = "Internal server error during migration" });
            }
        }

        private static string ExtractBaseSerialNumber(string serialNumber)
        {
            var suffixes = new[] { "-VT", "-PT", "-MT", "-RT", "-UT" };
            foreach (var suffix in suffixes)
            {
                if (serialNumber.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return serialNumber[..^suffix.Length];
                }
            }
            return serialNumber;
        }

        // Helper methods
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

        private static TraineeCertificateDto MapCertificateToDto(CertificateNew certificate)
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

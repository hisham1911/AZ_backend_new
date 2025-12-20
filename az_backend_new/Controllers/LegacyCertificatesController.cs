using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using az_backend_new.DTOs;
using az_backend_new.Models;
using az_backend_new.Repositories;

namespace az_backend_new.Controllers
{
    /// <summary>
    /// Controller للتوافق مع الـ API القديم
    /// يحول البيانات من الهيكل الجديد (Trainee + Certificates) إلى التنسيق القديم
    /// </summary>
    [ApiController]
    [Route("api/v2/[controller]")]
    public class LegacyCertificatesController : ControllerBase
    {
        private readonly ITraineeRepository _traineeRepository;
        private readonly ILogger<LegacyCertificatesController> _logger;

        public LegacyCertificatesController(
            ITraineeRepository traineeRepository,
            ILogger<LegacyCertificatesController> logger)
        {
            _traineeRepository = traineeRepository;
            _logger = logger;
        }

        /// <summary>
        /// الحصول على جميع الشهادات بالتنسيق القديم
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<LegacyCertificateDto>>> GetCertificates(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _traineeRepository.GetAllWithCertificatesAsync(1, int.MaxValue);
                
                // تحويل إلى التنسيق القديم
                var legacyCertificates = result.Items
                    .SelectMany(t => t.Certificates.Select(c => MapToLegacyDto(t, c)))
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();

                var totalCount = legacyCertificates.Count;
                var pagedItems = legacyCertificates
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                return Ok(new PagedResult<LegacyCertificateDto>
                {
                    Items = pagedItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving legacy certificates");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// البحث عن الشهادات بالتنسيق القديم
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<LegacyCertificateDto>>> SearchCertificates(
            [FromQuery] string? serialNumber,
            [FromQuery] string? personName,
            [FromQuery] ServiceMethod? serviceMethod)
        {
            try
            {
                var searchDto = new TraineeSearchDto
                {
                    SerialNumber = serialNumber,
                    PersonName = personName,
                    ServiceMethod = serviceMethod,
                    Page = 1,
                    PageSize = 100
                };

                var trainees = await _traineeRepository.SearchAsync(searchDto);
                
                var legacyCertificates = trainees
                    .SelectMany(t => t.Certificates.Select(c => MapToLegacyDto(t, c)))
                    .ToList();

                // إذا كان البحث بالرقم التسلسلي، فلتر النتائج
                if (!string.IsNullOrEmpty(serialNumber))
                {
                    // إذا كان الرقم يحتوي على لاحقة الطريقة، ابحث بالتطابق التام
                    if (serialNumber.Contains("-"))
                    {
                        legacyCertificates = legacyCertificates
                            .Where(c => c.SerialNumber.Equals(serialNumber, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                }

                return Ok(legacyCertificates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching legacy certificates");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// الحصول على شهادة بالـ ID (ID الشهادة الجديد)
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<LegacyCertificateDto>> GetCertificate(int id)
        {
            try
            {
                var certificate = await _traineeRepository.GetCertificateByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                var trainee = await _traineeRepository.GetByIdAsync(certificate.TraineeId);
                if (trainee == null)
                {
                    return NotFound(new { message = "Trainee not found" });
                }

                return Ok(MapToLegacyDto(trainee, certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving legacy certificate {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// إنشاء شهادة بالتنسيق القديم (يُنشئ متدرب إذا لم يكن موجوداً)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<LegacyCertificateDto>> CreateCertificate(CreateCertificateDto createDto)
        {
            try
            {
                // استخراج الرقم التسلسلي الأساسي (بدون لاحقة الطريقة)
                var baseSerialNumber = ExtractBaseSerialNumber(createDto.SerialNumber);
                
                // البحث عن المتدرب بالرقم التسلسلي
                var trainee = await _traineeRepository.GetBySerialNumberWithCertificatesAsync(baseSerialNumber);
                
                if (trainee == null)
                {
                    // إنشاء متدرب جديد
                    trainee = new Trainee
                    {
                        SerialNumber = baseSerialNumber,
                        PersonName = createDto.PersonName,
                        Country = createDto.Country,
                        State = createDto.State,
                        StreetAddress = createDto.StreetAddress
                    };
                    trainee = await _traineeRepository.CreateAsync(trainee);
                }
                else
                {
                    // تحديث اسم المتدرب إذا تغير
                    if (trainee.PersonName != createDto.PersonName)
                    {
                        trainee.PersonName = createDto.PersonName;
                        await _traineeRepository.UpdateAsync(trainee);
                    }
                }

                // التحقق من عدم وجود شهادة بنفس الطريقة
                if (await _traineeRepository.TraineeHasCertificateWithMethodAsync(trainee.Id, createDto.ServiceMethod))
                {
                    return Conflict(new { message = $"Trainee already has a certificate for {createDto.ServiceMethod}" });
                }

                // إنشاء الشهادة
                var certificate = new CertificateNew
                {
                    ServiceMethod = createDto.ServiceMethod,
                    CertificateType = createDto.CertificateType,
                    ExpiryDate = DateTime.SpecifyKind(createDto.ExpiryDate, DateTimeKind.Utc)
                };

                certificate = await _traineeRepository.AddCertificateAsync(trainee.Id, certificate);

                _logger.LogInformation("Legacy certificate created: {SerialNumber}-{Method}", 
                    trainee.SerialNumber, certificate.MethodCode);

                return CreatedAtAction(
                    nameof(GetCertificate), 
                    new { id = certificate.Id }, 
                    MapToLegacyDto(trainee, certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating legacy certificate");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// تحديث شهادة بالتنسيق القديم
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<LegacyCertificateDto>> UpdateCertificate(int id, UpdateCertificateDto updateDto)
        {
            try
            {
                var certificate = await _traineeRepository.GetCertificateByIdAsync(id);
                if (certificate == null)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                var trainee = await _traineeRepository.GetByIdAsync(certificate.TraineeId);
                if (trainee == null)
                {
                    return NotFound(new { message = "Trainee not found" });
                }

                // تحديث بيانات المتدرب
                if (!string.IsNullOrEmpty(updateDto.PersonName))
                    trainee.PersonName = updateDto.PersonName;
                if (updateDto.Country != null)
                    trainee.Country = updateDto.Country;
                if (updateDto.State != null)
                    trainee.State = updateDto.State;
                if (updateDto.StreetAddress != null)
                    trainee.StreetAddress = updateDto.StreetAddress;

                await _traineeRepository.UpdateAsync(trainee);

                // تحديث بيانات الشهادة
                if (updateDto.ServiceMethod.HasValue)
                {
                    if (await _traineeRepository.TraineeHasCertificateWithMethodAsync(
                        trainee.Id, updateDto.ServiceMethod.Value, id))
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

                _logger.LogInformation("Legacy certificate updated: {Id}", id);

                return Ok(MapToLegacyDto(trainee, certificate));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating legacy certificate {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// حذف شهادة
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            try
            {
                var success = await _traineeRepository.DeleteCertificateAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Certificate not found" });
                }

                _logger.LogInformation("Legacy certificate deleted: {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting legacy certificate {Id}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // Helper methods
        private static LegacyCertificateDto MapToLegacyDto(Trainee trainee, CertificateNew certificate)
        {
            return new LegacyCertificateDto
            {
                Id = certificate.Id,
                SerialNumber = $"{trainee.SerialNumber}-{certificate.MethodCode}",
                PersonName = trainee.PersonName,
                ServiceMethod = certificate.ServiceMethod,
                CertificateType = certificate.CertificateType,
                ExpiryDate = certificate.ExpiryDate,
                Country = trainee.Country,
                State = trainee.State,
                StreetAddress = trainee.StreetAddress,
                CreatedAt = certificate.CreatedAt,
                UpdatedAt = certificate.UpdatedAt,
                IsExpired = certificate.IsExpired
            };
        }

        private static string ExtractBaseSerialNumber(string serialNumber)
        {
            // إزالة لاحقة الطريقة إذا وجدت
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
    }
}

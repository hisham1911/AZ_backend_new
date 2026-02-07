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
        /// رفع ملف إكسيل لاستيراد المتدربين والشهادات
        /// التنسيق: S/N | Name | VT_TYPE | VT_Expiry | PT_TYPE | PT_Expiry | MT_TYPE | MT_Expiry | RT_TYPE | RT_Expiry | UT_TYPE | UT_Expiry
        /// </summary>
        [HttpPost("import")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest(new { message = "Invalid file format. Please upload an Excel file (.xlsx or .xls)" });

            try
            {
                var importedTrainees = 0;
                var importedCertificates = 0;
                var updatedTrainees = 0;
                var errors = new List<string>();
                var rowsWithData = 0;
                var emptyRows = 0;
                int startRow = 2;
                int totalRows = 0;
                
                // تتبع السجلات في الملف الحالي
                var importSummary = new Dictionary<string, TraineeImportInfo>();

                using (var stream = file.OpenReadStream())
                {
                    using var reader = ExcelReaderFactory.CreateReader(stream);
                    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                    {
                        ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
                    });

                    if (dataSet.Tables.Count == 0)
                        return BadRequest(new { message = "Excel file is empty" });

                    var table = dataSet.Tables[0];
                    
                    _logger.LogInformation("Excel: {Rows} rows, {Cols} columns", table.Rows.Count, table.Columns.Count);

                    // ====== تحليل ذكي لبنية الملف ======
                    int nameCol = -1;
                    int serialCol = -1;
                    int vtTypeCol = -1;
                    int vtExpiryCol = -1;
                    totalRows = table.Rows.Count;

                    // البحث عن صف العناوين
                    for (int r = 0; r < Math.Min(5, table.Rows.Count); r++)
                    {
                        var row = table.Rows[r];
                        for (int c = 0; c < table.Columns.Count; c++)
                        {
                            var cellValue = row[c]?.ToString()?.Trim().ToUpperInvariant() ?? "";
                            
                            if (cellValue.Contains("NAME") || cellValue == "الاسم")
                                nameCol = c;
                            else if (cellValue == "S/N" || cellValue == "SERIAL" || cellValue == "رقم" || cellValue == "م")
                                serialCol = c;
                            else if (cellValue == "VT" && vtTypeCol == -1)
                            {
                                // VT header found, next row should have TYPE and Expiry
                                vtTypeCol = c;
                                vtExpiryCol = c + 1;
                            }
                            else if (cellValue == "TYPE" && vtTypeCol == -1)
                            {
                                vtTypeCol = c;
                                vtExpiryCol = c + 1;
                            }
                        }
                        
                        // إذا وجدنا الاسم، الصف التالي بعد العناوين هو بداية البيانات
                        if (nameCol >= 0)
                        {
                            startRow = r + 1;
                            // تحقق إذا كان هناك صف عناوين ثاني (TYPE, Expiry Date)
                            if (startRow < table.Rows.Count)
                            {
                                var nextRow = table.Rows[startRow];
                                var firstCell = nextRow[0]?.ToString()?.Trim().ToUpperInvariant() ?? "";
                                if (firstCell.Contains("TYPE") || string.IsNullOrEmpty(firstCell))
                                {
                                    startRow++; // تخطي صف العناوين الثاني
                                }
                            }
                            break;
                        }
                    }

                    // إذا لم نجد الأعمدة، نستخدم الافتراضي
                    if (nameCol == -1)
                    {
                        // افتراضي: العمود الأول S/N، الثاني Name
                        if (table.Rows.Count > 2)
                        {
                            var testRow = table.Rows[2];
                            var firstVal = testRow[0]?.ToString()?.Trim() ?? "";
                            if (int.TryParse(firstVal, out _))
                            {
                                serialCol = 0;
                                nameCol = 1;
                                vtTypeCol = 2;
                                vtExpiryCol = 3;
                            }
                            else
                            {
                                nameCol = 0;
                                vtTypeCol = 1;
                                vtExpiryCol = 2;
                            }
                        }
                        startRow = 2; // تخطي صفين عناوين
                    }

                    // إذا لم يتم تحديد vtTypeCol، نحسبه من nameCol
                    if (vtTypeCol == -1)
                    {
                        vtTypeCol = nameCol + 1;
                        vtExpiryCol = nameCol + 2;
                    }

                    _logger.LogInformation("Detected columns - Serial: {Serial}, Name: {Name}, VT_Type: {VTType}, VT_Expiry: {VTExpiry}, StartRow: {StartRow}",
                        serialCol, nameCol, vtTypeCol, vtExpiryCol, startRow);

                    // ====== معالجة البيانات ======
                    for (int row = startRow; row < table.Rows.Count; row++)
                    {
                        try
                        {
                            var dataRow = table.Rows[row];
                            
                            // ====== فحص إذا كان الصف يحتوي على بيانات ======
                            bool hasAnyData = false;
                            for (int c = 0; c < Math.Min(12, table.Columns.Count); c++)
                            {
                                var cellVal = dataRow[c]?.ToString()?.Trim() ?? "";
                                if (!string.IsNullOrEmpty(cellVal))
                                {
                                    hasAnyData = true;
                                    break;
                                }
                            }
                            
                            if (!hasAnyData)
                            {
                                emptyRows++;
                                continue;
                            }
                            
                            rowsWithData++;

                            // قراءة اسم الشخص وتنظيفه
                            var rawName = dataRow[nameCol]?.ToString() ?? "";
                            var personName = string.Join(" ", rawName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Trim();

                            if (string.IsNullOrEmpty(personName))
                            {
                                // تجاهل الصفوف التي لا تحتوي على اسم، حتى لو بها بيانات أخرى، لأن الاسم أساسي
                                continue; 
                            }

                            // قراءة الرقم التسلسلي
                            var serialNumber = "";
                            if (serialCol >= 0)
                            {
                                serialNumber = dataRow[serialCol]?.ToString()?.Trim() ?? "";
                            }
                            // إذا لم يوجد رقم تسلسلي، نتجاهل الصف إذا كنا نريد الصرامة، أو نولد واحداً.
                            // بناء على طلبك: "عدم الالتفات للصف الذي به رقم تسلسلي لكن بدون بيانات"
                            // سنقوم بالتحقق من الشهادات أولاً.

                            if (string.IsNullOrEmpty(serialNumber))
                            {
                                serialNumber = $"AZ-{DateTime.UtcNow:yyyyMMdd}-{row + 1:D4}";
                            }

                            // ====== التحقق من الشهادات ======
                            var certificates = new List<Certificate>();
                            // قائمة للأخطاء الداخلية للصف (للتشخيص فقط، لن تظهر إلا إذا قررنا ذلك)
                            var certErrors = new List<string>();

                            // دالة مساعدة لقراءة وإضافة الشهادة
                            void AddCertIfValid(ServiceMethod m, int tCol, int eCol)
                            {
                                var cert = TryCreateCertificateWithDetails(m, dataRow, tCol, eCol, out var err);
                                if (cert != null) certificates.Add(cert);
                            }

                            AddCertIfValid(ServiceMethod.VisualTesting, vtTypeCol, vtExpiryCol);
                            AddCertIfValid(ServiceMethod.LiquidPenetrantTesting, vtTypeCol + 2, vtExpiryCol + 2);
                            AddCertIfValid(ServiceMethod.MagneticParticleTesting, vtTypeCol + 4, vtExpiryCol + 4);
                            AddCertIfValid(ServiceMethod.RadiographicTesting, vtTypeCol + 6, vtExpiryCol + 6);
                            AddCertIfValid(ServiceMethod.UltrasonicTesting, vtTypeCol + 8, vtExpiryCol + 8);

                            if (certificates.Count == 0)
                            {
                                // التغيير المطلوب: "عدم الالتفات للصف الذي به رقم تسلسلي لكن بدون اي بيانات اخري"
                                // إذا لم توجد شهادات صالحة، نعتبر هذا الصف غير مفيد ونتجاهله بصمت (بدون خطأ).
                                emptyRows++; // نحتسبه كصف فارغ أو "مهمل"
                                continue;
                            }

                            var existingTrainee = await _traineeRepository.GetBySerialNumberWithCertificatesAsync(serialNumber);

                            // 1. تتبع السجل في ملخص الاستيراد (مبكراً قبل الحفظ للتمكن من رؤية التفاصيل حتى لو فشل الحفظ)
                            var itemStatus = existingTrainee != null ? "Update" : "New";
                            if (!importSummary.ContainsKey(serialNumber))
                            {
                                importSummary[serialNumber] = new TraineeImportInfo 
                                { 
                                    Name = personName, 
                                    Status = itemStatus, 
                                    Rows = new List<int> { row + 1 } 
                                };
                            }
                            else
                            {
                                importSummary[serialNumber].Rows.Add(row + 1);
                                // إذا وجد السجل مرتين في نفس الملف، نحدث الحالة لتكون تحديثاً في المرة الثانية
                                importSummary[serialNumber].Status = "Update (Duplicate in file)";
                            }

                            // 2. محاولة الحفظ في قاعدة البيانات
                            try 
                            {
                                if (existingTrainee != null)
                                {
                                    // تحديث الاسم إذا تغير
                                    existingTrainee.PersonName = personName;
                                    
                                    // تحديث أو إضافة الشهادات
                                    foreach (var newCert in certificates)
                                    {
                                        var existingCert = existingTrainee.Certificates
                                            .FirstOrDefault(c => c.ServiceMethod == newCert.ServiceMethod);
                                        
                                        if (existingCert != null)
                                        {
                                            existingCert.ExpiryDate = newCert.ExpiryDate;
                                            existingCert.CertificateType = newCert.CertificateType;
                                            existingCert.UpdatedAt = DateTime.UtcNow;
                                        }
                                        else
                                        {
                                            existingTrainee.Certificates.Add(newCert);
                                        }
                                    }
                                    
                                    await _traineeRepository.UpdateAsync(existingTrainee);
                                    updatedTrainees++;
                                    importedCertificates += certificates.Count;
                                }
                                else
                                {
                                    // إنشاء متدرب جديد تماماً
                                    var trainee = new Trainee
                                    {
                                        SerialNumber = serialNumber,
                                        PersonName = personName,
                                        Certificates = certificates
                                    };

                                    await _traineeRepository.CreateAsync(trainee);
                                    importedTrainees++;
                                    importedCertificates += certificates.Count;
                                }
                            }
                            catch (Exception saveEx)
                            {
                                // إذا فشل الحفظ، نحدث الحالة في الملخص لتوضيح الفشل
                                if (importSummary.ContainsKey(serialNumber))
                                {
                                    importSummary[serialNumber].Status = "Failed to save";
                                }
                                throw; // نلقي الخطأ ليتم التقاطه في الـ catch الخارجي للصف
                            }
                        }
                        catch (Exception ex)
                        {
                            var msg = ex.Message;
                            var currentEx = ex;
                            while (currentEx.InnerException != null)
                            {
                                msg += " -> " + currentEx.InnerException.Message;
                                currentEx = currentEx.InnerException;
                            }
                            _logger.LogError(ex, "Error processing row {Row}", row + 1);
                            errors.Add($"صف {row + 1}: {msg}");
                        }
                    }
                }

                _logger.LogInformation("Import combined: {New} new, {Updated} updated, {Certs} certificates", 
                    importedTrainees, updatedTrainees, importedCertificates);

                return Ok(new
                {
                    message = (importedTrainees + updatedTrainees) > 0 ? "اكتملت عملية المعالجة بنجاح" : "لم يتم استيراد أي بيانات في قاعدة البيانات. انظر جدول الأخطاء أدناه.",
                    importedTrainees,
                    updatedTrainees,
                    importedCertificates,
                    analysis = new
                    {
                        totalRowsInFile = totalRows,
                        rowsWithData,
                        emptyRows,
                        uniqueTraineesInFile = importSummary.Count
                    },
                    detailedSummary = importSummary.Select(x => new {
                        serialNumber = x.Key,
                        name = x.Value.Name,
                        status = x.Value.Status,
                        rows = x.Value.Rows
                    }).OrderBy(x => x.rows[0]).ToList(),
                    errors = errors.Take(50).ToList(),
                    totalErrors = errors.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Excel file");
                return StatusCode(500, new { message = "خطأ في معالجة الملف: " + ex.Message });
            }
        }

        private class TraineeImportInfo
        {
            public string Name { get; set; } = "";
            public string Status { get; set; } = "";
            public List<int> Rows { get; set; } = new();
        }

        /// <summary>
        /// حذف جميع البيانات (متدربين وشهادات)
        /// </summary>
        [HttpDelete("delete-all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                await _traineeRepository.DeleteAllAsync();
                return Ok(new { message = "تم حذف جميع البيانات بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all data");
                return StatusCode(500, new { message = "خطأ في عملية الحذف: " + ex.Message });
            }
        }
        /// <summary>
        /// تصدير بيانات المتدربين والشهادات إلى ملف Excel
        /// </summary>

        /// <summary>
        /// تنظيف قاعدة البيانات من السجلات المكررة (التي تحتوي على توقيت زمني) ودمج بياناتها
        /// </summary>
        [HttpPost("cleanup")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupDuplicates()
        {
            try
            {
                // 1. جلب كل المتدربين
                var allTrainees = await _traineeRepository.GetAllWithCertificatesAsync(1, 100000);
                var trainees = allTrainees.Items;
                
                int cleanedCount = 0;
                int mergedCount = 0;
                int fixedCount = 0;

                // 2. تصفية السجلات التي تبدو مكررة (تحتوي على شرطة وأرقام في النهاية)
                // النمط المتوقع: أي شيء ينتهي بـ -123456 (6 أرقام)
                var duplicates = trainees
                    .Where(t => System.Text.RegularExpressions.Regex.IsMatch(t.SerialNumber, @"-\d{6}$"))
                    .ToList();

                foreach (var duplicate in duplicates)
                {
                    // استخراج الرقم الأصلي (إزالة آخر 7 محارف: الشرطة + 6 أرقام)
                    var originalSerial = duplicate.SerialNumber.Substring(0, duplicate.SerialNumber.Length - 7);
                    
                    // البحث عن السجل الأصلي
                    var originalTrainee = await _traineeRepository.GetBySerialNumberWithCertificatesAsync(originalSerial);

                    if (originalTrainee != null)
                    {
                        // السيناريو A: الأصلي موجود -> دمج الشهادات وحذف المكرر
                        bool changed = false;
                        foreach (var cert in duplicate.Certificates)
                        {
                            // هل الشهادة موجودة لدى الأصلي؟
                            var existingCert = originalTrainee.Certificates
                                .FirstOrDefault(c => c.ServiceMethod == cert.ServiceMethod);

                            if (existingCert == null)
                            {
                                // غير موجودة -> ننقلها للأصلي
                                cert.TraineeId = originalTrainee.Id;
                                originalTrainee.Certificates.Add(cert);
                                changed = true;
                            }
                            else
                            {
                                // موجودة -> نحدث التاريخ فقط إذا كان الجديد أحدث
                                if (cert.ExpiryDate > existingCert.ExpiryDate)
                                {
                                    existingCert.ExpiryDate = cert.ExpiryDate;
                                    existingCert.CertificateType = cert.CertificateType;
                                    existingCert.UpdatedAt = DateTime.UtcNow;
                                    changed = true;
                                }
                            }
                        }

                        if (changed)
                        {
                            await _traineeRepository.UpdateAsync(originalTrainee);
                            mergedCount++;
                        }

                        // حذف السجل المكرر بالكامل
                        await _traineeRepository.DeleteAsync(duplicate.Id);
                        cleanedCount++;
                    }
                    else
                    {
                        // السيناريو B: الأصلي غير موجود -> تصحيح الرقم للسجل الحالي
                        duplicate.SerialNumber = originalSerial;
                        await _traineeRepository.UpdateAsync(duplicate);
                        fixedCount++;
                    }
                }

                return Ok(new
                {
                    message = "تمت عملية التنظيف بنجاح",
                    totalProcessed = duplicates.Count,
                    mergedWithOriginal = mergedCount,
                    renamedToOriginal = fixedCount,
                    deletedDuplicates = cleanedCount,
                    remainingTrainees = trainees.Count - cleanedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up duplicates");
                return StatusCode(500, new { message = "خطأ في عملية التنظيف: " + ex.Message });
            }
        }


        [HttpGet("export")]
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                // جلب جميع البيانات بدون تقسيم صفحات
                // ملاحظة: مع العدد الكبير جداً قد نحتاج لطريقة أخرى، لكن لعدة آلاف هذا يعمل بامتياز
                var trainees = await _traineeRepository.GetAllWithCertificatesAsync(1, 100000); // 100k limit safety

                using (var workbook = new ClosedXML.Excel.XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Trainees");

                    // 1. إعداد العناوين
                    // صف العناوين الرئيسية
                    worksheet.Cell(1, 1).Value = "S/N";
                    worksheet.Cell(1, 2).Value = "Name";
                    
                    var methodHeaders = new[] { "VT", "PT", "MT", "RT", "UT" };
                    int startCol = 3;
                    
                    for (int i = 0; i < methodHeaders.Length; i++)
                    {
                        // عنوان الطريقة (VT, PT, etc) - merge على عمودين فقط
                        worksheet.Range(1, startCol, 1, startCol + 1).Merge();
                        worksheet.Cell(1, startCol).Value = methodHeaders[i];
                        worksheet.Cell(1, startCol).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        worksheet.Cell(1, startCol).Style.Font.Bold = true;
                        worksheet.Cell(1, startCol).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(91, 155, 213);
                        worksheet.Cell(1, startCol).Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                        
                        // الصف الثاني: TYPE و Expiry Date
                        worksheet.Cell(2, startCol).Value = "TYPE";
                        worksheet.Cell(2, startCol + 1).Value = "Expiry Date";
                        worksheet.Cell(2, startCol).Style.Font.Bold = true;
                        worksheet.Cell(2, startCol + 1).Style.Font.Bold = true;
                        worksheet.Cell(2, startCol).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                        worksheet.Cell(2, startCol + 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                        
                        startCol += 2;
                    }
                    
                    // تنسيق أعمدة S/N و Name
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Cell(1, 1).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                    worksheet.Cell(1, 2).Style.Font.Bold = true;
                    worksheet.Cell(1, 2).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                    worksheet.Range(1, 1, 2, 1).Merge();
                    worksheet.Range(1, 2, 2, 2).Merge();
                    worksheet.Cell(1, 1).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;
                    worksheet.Cell(1, 2).Style.Alignment.Vertical = ClosedXML.Excel.XLAlignmentVerticalValues.Center;

                    // 2. تعبئة البيانات
                    int row = 3;
                    var today = DateTime.UtcNow.Date;
                    
                    foreach (var trainee in trainees.Items)
                    {
                        worksheet.Cell(row, 1).Value = trainee.SerialNumber;
                        worksheet.Cell(row, 2).Value = trainee.PersonName;

                        // تعبئة الشهادات في الأعمدة المناسبة
                        foreach (var cert in trainee.Certificates)
                        {
                            int colIndex = -1;
                            switch (cert.ServiceMethod)
                            {
                                case ServiceMethod.VisualTesting: colIndex = 3; break;           // VT
                                case ServiceMethod.LiquidPenetrantTesting: colIndex = 5; break; // PT
                                case ServiceMethod.MagneticParticleTesting: colIndex = 7; break;// MT
                                case ServiceMethod.RadiographicTesting: colIndex = 9; break;    // RT
                                case ServiceMethod.UltrasonicTesting: colIndex = 11; break;     // UT
                            }

                            if (colIndex != -1)
                            {
                                var typeStr = cert.CertificateType == CertificateType.Recertificate ? "Recertificate" : "Issue date";
                                var typeCell = worksheet.Cell(row, colIndex);
                                var dateCell = worksheet.Cell(row, colIndex + 1);
                                
                                // تعيين القيم
                                typeCell.Value = typeStr;
                                dateCell.Value = cert.ExpiryDate.ToString("M/d/yyyy");
                                
                                // تلوين خلية TYPE
                                // أخضر فاتح للـ Recertificate
                                if (cert.CertificateType == CertificateType.Recertificate)
                                {
                                    typeCell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(198, 239, 206); // أخضر فاتح
                                }
                                
                                // تلوين خلية التاريخ حسب الحالة
                                var expiryDate = cert.ExpiryDate.Date;
                                var daysUntilExpiry = (expiryDate - today).TotalDays;
                                
                                if (daysUntilExpiry < 0)
                                {
                                    // منتهية - أحمر
                                    dateCell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(255, 199, 206);
                                    dateCell.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(156, 0, 6);
                                }
                                else if (daysUntilExpiry <= 180) // 6 أشهر
                                {
                                    // قريبة من الانتهاء - أصفر
                                    dateCell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(255, 235, 156);
                                    dateCell.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(156, 101, 0);
                                }
                                else
                                {
                                    // المستقبل البعيد - أزرق فاتح
                                    dateCell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(155, 194, 230);
                                    dateCell.Style.Font.FontColor = ClosedXML.Excel.XLColor.FromArgb(0, 61, 143);
                                }
                                
                                // محاذاة النصوص في المنتصف
                                typeCell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                                dateCell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                            }
                        }
                        row++;
                    }

                    // إضافة حدود للجدول
                    var dataRange = worksheet.Range(1, 1, row - 1, 12);
                    dataRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                    
                    // تنسيق عرض الأعمدة
                    worksheet.Column(1).Width = 8;  // S/N
                    worksheet.Column(2).Width = 50; // Name - أوسع للأسماء الطويلة جداً
                    for (int i = 3; i <= 12; i++)
                    {
                        if ((i - 3) % 2 == 0) // أعمدة TYPE
                            worksheet.Column(i).Width = 15;
                        else // أعمدة Expiry Date
                            worksheet.Column(i).Width = 12;
                    }

                    // تحضير الملف للتحميل
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Trainees_Export_{DateTime.Now:yyyyMMdd}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Excel file");
                return StatusCode(500, new { message = "خطأ في تصدير الملف: " + ex.Message });
            }
        }



        private static string GetRowPreview(System.Data.DataRow row)
        {
            var preview = new List<string>();
            for (int i = 0; i < Math.Min(6, row.Table.Columns.Count); i++)
            {
                var val = row[i]?.ToString()?.Trim() ?? "";
                if (!string.IsNullOrEmpty(val))
                    preview.Add($"Col{i}:{val.Substring(0, Math.Min(15, val.Length))}");
            }
            return string.Join(", ", preview);
        }

        private static Certificate? TryCreateCertificateWithDetails(ServiceMethod method, System.Data.DataRow dataRow, int typeCol, int expiryCol, out string? error)
        {
            error = null;
            try
            {
                if (dataRow.Table.Columns.Count <= expiryCol)
                {
                    return null;
                }

                var typeStr = dataRow[typeCol]?.ToString()?.Trim() ?? "";
                var expiryValue = dataRow[expiryCol];
                var expiryStr = expiryValue?.ToString()?.Trim() ?? "";

                if (string.IsNullOrEmpty(typeStr) && string.IsNullOrEmpty(expiryStr))
                    return null;

                if (!string.IsNullOrEmpty(typeStr) && string.IsNullOrEmpty(expiryStr))
                {
                    error = $"يوجد نوع '{typeStr}' لكن لا يوجد تاريخ انتهاء";
                    return null;
                }

                DateTime? expiryDate = null;

                if (expiryValue is DateTime dt)
                {
                    expiryDate = dt;
                }
                else if (!string.IsNullOrEmpty(expiryStr))
                {
                    if (double.TryParse(expiryStr, out double oaDate) && oaDate > 1 && oaDate < 100000)
                    {
                        try { expiryDate = DateTime.FromOADate(oaDate); } catch { }
                    }
                    
                    if (expiryDate == null)
                    {
                        if (DateTime.TryParse(expiryStr, out DateTime parsed))
                        {
                            expiryDate = parsed;
                        }
                        else
                        {
                            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy", "yyyy/MM/dd" };
                            foreach (var fmt in formats)
                            {
                                if (DateTime.TryParseExact(expiryStr, fmt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime exactParsed))
                                {
                                    expiryDate = exactParsed;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (expiryDate == null)
                {
                    error = $"تاريخ غير صالح: '{expiryStr}'";
                    return null;
                }

                var certType = CertificateType.Initial;
                if (!string.IsNullOrEmpty(typeStr))
                {
                    var upper = typeStr.ToUpperInvariant();
                    if (upper.Contains("RECERT") || upper.Contains("RE-CERT") || upper == "R" || upper == "2")
                        certType = CertificateType.Recertificate;
                }

                return new Certificate
                {
                    ServiceMethod = method,
                    CertificateType = certType,
                    ExpiryDate = DateTime.SpecifyKind(expiryDate.Value, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                error = $"خطأ: {ex.Message}";
                return null;
            }
        }

        private static Certificate? TryCreateCertificate(ServiceMethod method, System.Data.DataRow dataRow, int typeCol, int expiryCol, out string? error)
        {
            error = null;
            try
            {
                if (dataRow.Table.Columns.Count <= expiryCol)
                {
                    error = $"Not enough columns (need {expiryCol + 1}, have {dataRow.Table.Columns.Count})";
                    return null;
                }

                var typeStr = dataRow[typeCol]?.ToString()?.Trim() ?? "";
                var expiryValue = dataRow[expiryCol];

                if (string.IsNullOrEmpty(typeStr) && (expiryValue == null || string.IsNullOrEmpty(expiryValue.ToString())))
                    return null; // لا يوجد بيانات - ليس خطأ

                // تحويل التاريخ
                DateTime? expiryDate = null;

                if (expiryValue is DateTime dt)
                {
                    expiryDate = dt;
                }
                else if (expiryValue != null)
                {
                    var expiryStr = expiryValue.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(expiryStr))
                    {
                        // محاولة تحويل من رقم Excel
                        if (double.TryParse(expiryStr, out double oaDate) && oaDate > 1 && oaDate < 100000)
                        {
                            expiryDate = DateTime.FromOADate(oaDate);
                        }
                        else if (DateTime.TryParse(expiryStr, out DateTime parsed))
                        {
                            expiryDate = parsed;
                        }
                        else
                        {
                            // محاولة تنسيقات مختلفة
                            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "yyyy-MM-dd" };
                            foreach (var fmt in formats)
                            {
                                if (DateTime.TryParseExact(expiryStr, fmt, 
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.None, out DateTime exactParsed))
                                {
                                    expiryDate = exactParsed;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (expiryDate == null)
                    return null;

                // تحديد نوع الشهادة
                var certType = CertificateType.Initial;
                if (!string.IsNullOrEmpty(typeStr))
                {
                    var upper = typeStr.ToUpperInvariant();
                    if (upper.Contains("RECERT"))
                        certType = CertificateType.Recertificate;
                }

                return new Certificate
                {
                    ServiceMethod = method,
                    CertificateType = certType,
                    ExpiryDate = DateTime.SpecifyKind(expiryDate.Value, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        private static Certificate? CreateCertificate(ServiceMethod method, string typeStr, string expiryStr)
        {
            try
            {
                var certType = ParseCertificateType(typeStr);
                var expiryDate = ParseExpiryDate(expiryStr);
                
                if (expiryDate == null)
                    return null;

                return new Certificate
                {
                    ServiceMethod = method,
                    CertificateType = certType,
                    ExpiryDate = DateTime.SpecifyKind(expiryDate.Value, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        private static Certificate? CreateCertificateFlexible(string typeOrMethod, string expiryStr)
        {
            try
            {
                // محاولة تحديد نوع الشهادة من النص
                var method = ParseServiceMethod(typeOrMethod);
                var certType = ParseCertificateType(typeOrMethod);
                var expiryDate = ParseExpiryDate(expiryStr);
                
                if (expiryDate == null)
                    return null;

                return new Certificate
                {
                    ServiceMethod = method,
                    CertificateType = certType,
                    ExpiryDate = DateTime.SpecifyKind(expiryDate.Value, DateTimeKind.Utc),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ParseExpiryDate(string expiryStr)
        {
            if (string.IsNullOrEmpty(expiryStr))
                return null;

            DateTime expiryDate;
            
            // محاولة تحويل التاريخ من رقم Excel (OLE Automation date)
            // Excel يخزن التواريخ كأرقام عشرية
            if (double.TryParse(expiryStr, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out double oaDate))
            {
                try
                {
                    // التحقق من أن الرقم في نطاق معقول لتاريخ Excel
                    // تواريخ Excel تبدأ من 1 يناير 1900
                    if (oaDate > 1 && oaDate < 100000)
                    {
                        return DateTime.FromOADate(oaDate);
                    }
                }
                catch
                {
                    // تجاهل الخطأ ومحاولة التنسيقات الأخرى
                }
            }
            
            // محاولة التحويل المباشر
            if (DateTime.TryParse(expiryStr, out expiryDate))
                return expiryDate;

            // محاولة تحويل التاريخ بتنسيقات مختلفة
            var formats = new[] { 
                "dd/MM/yyyy", "MM/dd/yyyy", "yyyy-MM-dd", 
                "d/M/yyyy", "M/d/yyyy",
                "dd-MM-yyyy", "MM-dd-yyyy",
                "dd.MM.yyyy", "MM.dd.yyyy",
                "yyyy/MM/dd", "yyyy.MM.dd",
                "d-M-yyyy", "M-d-yyyy"
            };
            
            if (DateTime.TryParseExact(expiryStr, formats, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out expiryDate))
            {
                return expiryDate;
            }

            return null;
        }

        private static ServiceMethod ParseServiceMethod(string value)
        {
            if (string.IsNullOrEmpty(value))
                return ServiceMethod.VisualTesting;

            var upper = value.ToUpperInvariant();
            
            if (upper.Contains("VT") || upper.Contains("VISUAL"))
                return ServiceMethod.VisualTesting;
            if (upper.Contains("PT") || upper.Contains("PENETRANT") || upper.Contains("DYE"))
                return ServiceMethod.LiquidPenetrantTesting;
            if (upper.Contains("MT") || upper.Contains("MAGNETIC"))
                return ServiceMethod.MagneticParticleTesting;
            if (upper.Contains("RT") || upper.Contains("RADIO") || upper.Contains("X-RAY"))
                return ServiceMethod.RadiographicTesting;
            if (upper.Contains("UT") || upper.Contains("ULTRA"))
                return ServiceMethod.UltrasonicTesting;

            return ServiceMethod.VisualTesting;
        }

        private static CertificateType ParseCertificateType(string value)
        {
            if (string.IsNullOrEmpty(value))
                return CertificateType.Initial;

            if (int.TryParse(value, out int num))
                return (CertificateType)num;
            
            var upper = value.ToUpperInvariant().Trim();
            
            if (upper.Contains("RECERT") || upper.Contains("RE-CERT") || upper == "2")
                return CertificateType.Recertificate;
            
            return CertificateType.Initial;
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

using az_backend_new.Models;
using System.ComponentModel.DataAnnotations;

namespace az_backend_new.DTOs
{
    /// <summary>
    /// DTO للمتدرب مع شهاداته
    /// </summary>
    public class TraineeDto
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string PersonName { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? StreetAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<TraineeCertificateDto> Certificates { get; set; } = new();
    }

    /// <summary>
    /// DTO للشهادة داخل المتدرب
    /// </summary>
    public class TraineeCertificateDto
    {
        public int Id { get; set; }
        public ServiceMethod ServiceMethod { get; set; }
        public string MethodCode { get; set; } = string.Empty;
        public CertificateType CertificateType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsExpired { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO لإنشاء متدرب جديد مع شهاداته
    /// </summary>
    public class CreateTraineeDto
    {
        [Required(ErrorMessage = "Serial Number is required")]
        [StringLength(50, ErrorMessage = "Serial Number cannot exceed 50 characters")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Person Name is required")]
        [StringLength(100, ErrorMessage = "Person Name cannot exceed 100 characters")]
        public string PersonName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(200)]
        public string? StreetAddress { get; set; }

        // الشهادات المراد إنشاؤها مع المتدرب
        public List<CreateTraineeCertificateDto> Certificates { get; set; } = new();
    }

    /// <summary>
    /// DTO لإنشاء شهادة للمتدرب
    /// </summary>
    public class CreateTraineeCertificateDto
    {
        [Required(ErrorMessage = "Service Method is required")]
        public ServiceMethod ServiceMethod { get; set; }

        [Required(ErrorMessage = "Certificate Type is required")]
        public CertificateType CertificateType { get; set; }

        [Required(ErrorMessage = "Expiry Date is required")]
        public DateTime ExpiryDate { get; set; }
    }

    /// <summary>
    /// DTO لتحديث بيانات المتدرب
    /// </summary>
    public class UpdateTraineeDto
    {
        [StringLength(50)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? PersonName { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        [StringLength(200)]
        public string? StreetAddress { get; set; }
    }

    /// <summary>
    /// DTO لإضافة شهادة لمتدرب موجود
    /// </summary>
    public class AddCertificateToTraineeDto
    {
        [Required(ErrorMessage = "Service Method is required")]
        public ServiceMethod ServiceMethod { get; set; }

        [Required(ErrorMessage = "Certificate Type is required")]
        public CertificateType CertificateType { get; set; }

        [Required(ErrorMessage = "Expiry Date is required")]
        public DateTime ExpiryDate { get; set; }
    }

    /// <summary>
    /// DTO لتحديث شهادة
    /// </summary>
    public class UpdateTraineeCertificateDto
    {
        public ServiceMethod? ServiceMethod { get; set; }
        public CertificateType? CertificateType { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// DTO للبحث عن المتدربين
    /// </summary>
    public class TraineeSearchDto
    {
        public string? SerialNumber { get; set; }
        public string? PersonName { get; set; }
        public ServiceMethod? ServiceMethod { get; set; }
        public bool? HasExpiredCertificates { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// DTO للتوافق مع الـ API القديم - يُرجع الشهادة بالتنسيق القديم
    /// </summary>
    public class LegacyCertificateDto
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty; // سيكون TraineeSerialNumber-MethodCode
        public string PersonName { get; set; } = string.Empty;
        public ServiceMethod ServiceMethod { get; set; }
        public CertificateType CertificateType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? StreetAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsExpired { get; set; }
    }
}

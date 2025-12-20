using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace az_backend_new.Models
{
    /// <summary>
    /// الشهادة - مرتبطة بمتدرب واحد
    /// كل شهادة تحتوي على نوع الخدمة وتاريخ الانتهاء
    /// </summary>
    public class CertificateNew
    {
        public int Id { get; set; }

        // Foreign Key للمتدرب
        [Required]
        public int TraineeId { get; set; }

        [Required(ErrorMessage = "Service Method is required")]
        public ServiceMethod ServiceMethod { get; set; }

        [Required(ErrorMessage = "Certificate Type is required")]
        public CertificateType CertificateType { get; set; }

        [Required(ErrorMessage = "Expiry Date is required")]
        public DateTime ExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation property - علاقة many-to-one مع المتدرب
        [ForeignKey("TraineeId")]
        public virtual Trainee Trainee { get; set; } = null!;

        // Helper property to check if certificate is expired
        public bool IsExpired => ExpiryDate < DateTime.UtcNow;

        // Helper property to get method code
        public string MethodCode => ServiceMethod switch
        {
            ServiceMethod.VisualTesting => "VT",
            ServiceMethod.LiquidPenetrantTesting => "PT",
            ServiceMethod.MagneticParticleTesting => "MT",
            ServiceMethod.RadiographicTesting => "RT",
            ServiceMethod.UltrasonicTesting => "UT",
            _ => "XX"
        };
    }
}

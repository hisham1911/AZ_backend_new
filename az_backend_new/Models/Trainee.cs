using System.ComponentModel.DataAnnotations;

namespace az_backend_new.Models
{
    /// <summary>
    /// المتدرب - يحتوي على البيانات الشخصية والرقم التسلسلي
    /// كل متدرب يمكن أن يكون لديه عدة شهادات
    /// </summary>
    public class Trainee
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Serial Number is required")]
        [StringLength(50, ErrorMessage = "Serial Number cannot exceed 50 characters")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Person Name is required")]
        [StringLength(100, ErrorMessage = "Person Name cannot exceed 100 characters")]
        public string PersonName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters")]
        public string? Country { get; set; }

        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
        public string? State { get; set; }

        [StringLength(200, ErrorMessage = "Street Address cannot exceed 200 characters")]
        public string? StreetAddress { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation property - علاقة one-to-many مع الشهادات
        public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    }
}

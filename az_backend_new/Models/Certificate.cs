using System.ComponentModel.DataAnnotations;

namespace az_backend_new.Models
{
    public class Certificate
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Serial Number is required")]
        [StringLength(50, ErrorMessage = "Serial Number cannot exceed 50 characters")]
        public string SerialNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Person Name is required")]
        [StringLength(100, ErrorMessage = "Person Name cannot exceed 100 characters")]
        public string PersonName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Service Method is required")]
        public ServiceMethod ServiceMethod { get; set; }

        [Required(ErrorMessage = "Certificate Type is required")]
        public CertificateType CertificateType { get; set; }

        [Required(ErrorMessage = "Expiry Date is required")]
        public DateTime ExpiryDate { get; set; }

        [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters")]
        public string? Country { get; set; }

        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
        public string? State { get; set; }

        [StringLength(200, ErrorMessage = "Street Address cannot exceed 200 characters")]
        public string? StreetAddress { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Helper property to check if certificate is expired
        public bool IsExpired => ExpiryDate < DateTime.UtcNow;
    }
}
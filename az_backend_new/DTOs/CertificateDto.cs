using az_backend_new.Models;
using System.ComponentModel.DataAnnotations;

namespace az_backend_new.DTOs
{
    public class CertificateDto
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
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

    public class CreateCertificateDto
    {
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
    }

    public class UpdateCertificateDto
    {
        [StringLength(50, ErrorMessage = "Serial Number cannot exceed 50 characters")]
        public string? SerialNumber { get; set; }

        [StringLength(100, ErrorMessage = "Person Name cannot exceed 100 characters")]
        public string? PersonName { get; set; }

        public ServiceMethod? ServiceMethod { get; set; }

        public CertificateType? CertificateType { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(50, ErrorMessage = "Country cannot exceed 50 characters")]
        public string? Country { get; set; }

        [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
        public string? State { get; set; }

        [StringLength(200, ErrorMessage = "Street Address cannot exceed 200 characters")]
        public string? StreetAddress { get; set; }
    }

    public class CertificateSearchDto
    {
        public string? SerialNumber { get; set; }
        public string? PersonName { get; set; }
        public ServiceMethod? ServiceMethod { get; set; }
        public bool? Expired { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }
}


    public class ImportResultDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessfulImports { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

using System.ComponentModel.DataAnnotations;

namespace az_backend_new.Models
{
    public enum CertificateType
    {
        [Display(Name = "Initial")]
        Initial = 1,

        [Display(Name = "Recertificate")]
        Recertificate = 2
    }
}
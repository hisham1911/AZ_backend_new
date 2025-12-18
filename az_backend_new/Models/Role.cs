using System.ComponentModel.DataAnnotations;

namespace az_backend_new.Models
{
    public enum Role
    {
        [Display(Name = "Admin")]
        Admin = 1,

        [Display(Name = "User")]
        User = 2
    }
}
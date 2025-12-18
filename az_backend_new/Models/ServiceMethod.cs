using System.ComponentModel.DataAnnotations;

namespace az_backend_new.Models
{
    public enum ServiceMethod
    {
        [Display(Name = "Visual Testing")]
        VisualTesting = 1,

        [Display(Name = "Liquid Penetrant Testing")]
        LiquidPenetrantTesting = 2,

        [Display(Name = "Magnetic Particle Testing")]
        MagneticParticleTesting = 3,

        [Display(Name = "Radiographic Testing")]
        RadiographicTesting = 4,

        [Display(Name = "Ultrasonic Testing")]
        UltrasonicTesting = 5
    }
}
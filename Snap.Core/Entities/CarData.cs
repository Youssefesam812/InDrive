using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Core.Entities
{
    public class CarData
    {
        [Key]
        public int Id { get; set; }
        public string CarPhoto { get; set; }
        public string LicenseFront { get; set; }
        public string LicenseBack { get; set; }
        public string CarBrand { get; set; }
        public string CarModel { get; set; }
        public string CarColor { get; set; }
        public string PlateNumber { get; set; }
        // Foreign key to Driver
        public int DriverId { get; set; }
        public Driver Driver { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Snap.Core.Entities
{
    public class Charge
    {
        [Key]
        public int Id { get; set; }
        public int DriverId { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public Driver Driver { get; set; }
    }
}

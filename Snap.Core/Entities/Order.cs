using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Core.Entities
{
    public class LatLng
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Link to user (AspNetUsers)
        [Required]
        public string UserId { get; set; } = null!;
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public DateTime Date { get; set; }

        [Required]
        public string From { get; set; } = null!;

        [Required]
        public string To { get; set; } = null!;

        public LatLng FromLatLng { get; set; } = new LatLng();
        public LatLng ToLatLng { get; set; } = new LatLng();

        public double ExpectedPrice { get; set; }

        // ride | delivery
        [Required]
        public string Type { get; set; } = null!;

        public double Distance { get; set; }

        public string? Notes { get; set; }

        public int NoPassengers { get; set; }
    }
}

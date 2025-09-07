using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Core.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Link to passenger (AspNetUsers)
        [Required]
        public string PassengerId { get; set; } = null!;
        [ForeignKey("PassengerId")]
        public User Passenger { get; set; } = null!;

        public DateTime Date { get; set; }

        [Required]
        public string From { get; set; } = null!;

        [Required]
        public string To { get; set; } = null!;

        public double ExpectedPrice { get; set; }

        // ride | delivery
        [Required]
        public string Type { get; set; } = null!;

        public double Distance { get; set; }

        public string? Notes { get; set; }

        public int NoPassengers { get; set; }
    }
}

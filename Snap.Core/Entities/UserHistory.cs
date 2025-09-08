using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Core.Entities
{
    public class UserHistory
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        [Required]
        public string From { get; set; }
        [Required]
        public string To { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public string PaymentMethod { get; set; }
        [Required]
        [RegularExpression("^(ride|delivery)$", ErrorMessage = "RideType must be either 'ride' or 'delivery'.")]
        public string RideType { get; set; } // "ride" or "delivery"
    }
}

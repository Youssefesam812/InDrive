using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Core.Entities
{
    public class TripsHistory
    {
        [Key]
        public int Id { get; set; }
        [Range(0, 5)]
        public int Review { get; set; }
        public string PaymentWay { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Date { get; set; }
        public double TotalTip { get; set; }

        // Foreign key to Driver
        public int DriverId { get; set; }
        [ForeignKey("DriverId")]
        public Driver Driver { get; set; }
    }
}

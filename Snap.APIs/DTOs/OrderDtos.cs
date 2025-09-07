using System;
using System.Text.Json.Serialization;

namespace Snap.APIs.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string PassengerId { get; set; } = null!;
        public DateTime Date { get; set; }
        public string From { get; set; } = null!;
        public string To { get; set; } = null!;
        public double ExpectedPrice { get; set; }
        public string Type { get; set; } = null!;
        public double Distance { get; set; }
        public string? Notes { get; set; }
        public int NoPassengers { get; set; }
    }

    public class CreateOrderDto
    {
        public string PassengerId { get; set; } = null!;
        public DateTime Date { get; set; }
        public string From { get; set; } = null!;
        public string To { get; set; } = null!;
        public double ExpectedPrice { get; set; }
        public string Type { get; set; } = null!;
        public double Distance { get; set; }
        public string? Notes { get; set; }
        public int NoPassengers { get; set; }
    }
}

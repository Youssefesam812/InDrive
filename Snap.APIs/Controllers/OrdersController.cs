using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snap.APIs.DTOs;
using Snap.APIs.Errors;
using Snap.Core.Entities;
using Snap.Repository.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Snap.APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly SnapDbContext _context;

        public OrdersController(SnapDbContext context)
        {
            _context = context;
        }

        // POST: api/Orders
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            if (dto == null)
                return BadRequest(new ApiResponse(400, "Invalid payload"));

            // Validate type
            var allowedTypes = new[] { "ride", "delivery" };
            if (string.IsNullOrWhiteSpace(dto.Type) || !allowedTypes.Contains(dto.Type.ToLower()))
                return BadRequest(new ApiResponse(400, "Invalid type. Allowed: ride, delivery"));

            // Validate user exists
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound(new ApiResponse(404, "User not found"));

            var order = new Order
            {
                UserId = dto.UserId,
                Date = dto.Date,
                From = dto.From,
                To = dto.To,
                FromLatLng = new LatLng { Lat = dto.FromLatLng.Lat, Lng = dto.FromLatLng.Lng },
                ToLatLng = new LatLng { Lat = dto.ToLatLng.Lat, Lng = dto.ToLatLng.Lng },
                ExpectedPrice = dto.ExpectedPrice,
                Type = dto.Type.ToLower(),
                Distance = dto.Distance,
                Notes = dto.Notes,
                NoPassengers = dto.NoPassengers
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var result = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                Date = order.Date,
                From = order.From,
                To = order.To,
                FromLatLng = new LatLngDto { Lat = order.FromLatLng.Lat, Lng = order.FromLatLng.Lng },
                ToLatLng = new LatLngDto { Lat = order.ToLatLng.Lat, Lng = order.ToLatLng.Lng },
                ExpectedPrice = order.ExpectedPrice,
                Type = order.Type,
                Distance = order.Distance,
                Notes = order.Notes,
                NoPassengers = order.NoPassengers
            };

            return Ok(result);
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<List<OrderDto>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Select(o => new OrderDto
                {
                    Id = o.Id,
                    UserId = o.UserId,
                    Date = o.Date,
                    From = o.From,
                    To = o.To,
                    FromLatLng = new LatLngDto { Lat = o.FromLatLng.Lat, Lng = o.FromLatLng.Lng },
                    ToLatLng = new LatLngDto { Lat = o.ToLatLng.Lat, Lng = o.ToLatLng.Lng },
                    ExpectedPrice = o.ExpectedPrice,
                    Type = o.Type,
                    Distance = o.Distance,
                    Notes = o.Notes,
                    NoPassengers = o.NoPassengers
                })
                .ToListAsync();

            return Ok(orders);
        }

        // GET: api/Orders/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrderById(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new ApiResponse(404, "Order not found"));

            var dto = new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                Date = order.Date,
                From = order.From,
                To = order.To,
                FromLatLng = new LatLngDto { Lat = order.FromLatLng.Lat, Lng = order.FromLatLng.Lng },
                ToLatLng = new LatLngDto { Lat = order.ToLatLng.Lat, Lng = order.ToLatLng.Lng },
                ExpectedPrice = order.ExpectedPrice,
                Type = order.Type,
                Distance = order.Distance,
                Notes = order.Notes,
                NoPassengers = order.NoPassengers
            };

            return Ok(dto);
        }

        // DELETE: api/Orders/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound(new ApiResponse(404, "Order not found"));

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

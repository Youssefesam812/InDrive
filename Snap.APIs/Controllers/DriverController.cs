using Microsoft.AspNetCore.Mvc;
using Snap.Core.Entities;
using Snap.APIs.DTOs;
using Snap.Repository.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Snap.APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly SnapDbContext _context;
        public DriverController(SnapDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDriver([FromBody] DriverDto dto)
        {
            var driver = new Driver
            {
                DriverPhoto = dto.DriverPhoto,
                DriverIdCard = dto.DriverIdCard,
                DriverLicenseFront = dto.DriverLicenseFront,
                DriverLicenseBack = dto.DriverLicenseBack,
                IdCardFront = dto.IdCardFront,
                IdCardBack = dto.IdCardBack,
                DriverFullname = dto.DriverFullname,
                NationalId = dto.NationalId,
                Age = dto.Age,
                LicenseNumber = dto.LicenseNumber,
                Email = dto.Email,
                Password = dto.Password,
                LicenseExpiryDate = dto.LicenseExpiryDate,
                UserId = dto.UserId
            };
            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();
            dto.Id = driver.Id;
            return Ok(dto);
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<DriverDto>> GetDriverByUserId(string userId)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
            if (driver == null) return NotFound();
            var avg = (double)driver.TotalReview / driver.NoReviews;
            if (avg > 5) avg = 5;
            var dto = new DriverDto
            {
                Id = driver.Id,
                DriverPhoto = driver.DriverPhoto,
                DriverIdCard = driver.DriverIdCard,
                DriverLicenseFront = driver.DriverLicenseFront,
                DriverLicenseBack = driver.DriverLicenseBack,
                IdCardFront = driver.IdCardFront,
                IdCardBack = driver.IdCardBack,
                DriverFullname = driver.DriverFullname,
                NationalId = driver.NationalId,
                Age = driver.Age,
                LicenseNumber = driver.LicenseNumber,
                Email = driver.Email,
                Password = driver.Password,
                LicenseExpiryDate = driver.LicenseExpiryDate,
                UserId = driver.UserId,
                Status = driver.Status.ToString(),
                Review = avg
            };
            return Ok(dto);
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingDrivers()
        {
            var pendingDrivers = await _context.Drivers
                .Where(d => d.Status == "pending")
                .Select(d => new DriverDto
                {
                    Id = d.Id,
                    DriverFullname = d.DriverFullname,
                    Email = d.Email,
                    Status = d.Status.ToString()
                })
                .ToListAsync();
            return Ok(pendingDrivers);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> ChangeDriverStatus(int id, [FromBody] ChangeDriverStatusDto dto)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            var allowed = new[] { "pending", "approved", "reject" };
            if (string.IsNullOrWhiteSpace(dto.Status) || !allowed.Contains(dto.Status.ToLower()))
                return BadRequest("Invalid status value. Allowed: pending, approved, reject.");

            driver.Status = dto.Status.ToLower();
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/review")]
        public async Task<IActionResult> AddReview(int id, [FromBody] AddDriverReviewDto dto)
        {
            if (dto.Review < 0 || dto.Review > 5)
                return BadRequest("Review must be between 0 and 5.");

            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            driver.TotalReview += dto.Review;
            driver.NoReviews += 1;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{id}/review")]
        public async Task<IActionResult> GetDriverReview(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            if (driver.NoReviews == 0) return Ok(0);

            var avg = (double)driver.TotalReview / driver.NoReviews;
            if (avg > 5) avg = 5;
            return Ok(avg);
        }

        // POST: api/driver/requestcharge
        [HttpPost("requestcharge")]
        public async Task<IActionResult> RequestCharge([FromBody] RequestChargeDto dto)
        {
            var driver = await _context.Drivers.FindAsync(dto.DriverId);
            if (driver == null) return NotFound("Driver not found");
            var charge = new Charge
            {
                DriverId = dto.DriverId,
                Name = dto.Name,
                Value = dto.Value,
                Image = dto.Image
            };
            _context.Charges.Add(charge);
            await _context.SaveChangesAsync();
            return Ok(charge);
        }

        // GET: api/driver/charges
        [HttpGet("charges")]
        public async Task<IActionResult> GetCharges()
        {
            var charges = await _context.Charges.Include(c => c.Driver).ToListAsync();
            return Ok(charges);
        }

        // POST: api/driver/charge/{id}/action
        [HttpPost("charge/{id}/action")]
        public async Task<IActionResult> HandleCharge(int id, [FromBody] ChargeActionDto dto)
        {
            var charge = await _context.Charges.Include(c => c.Driver).FirstOrDefaultAsync(c => c.Id == id);
            if (charge == null) return NotFound("Charge not found");
            if (dto.Action.ToLower() == "approve")
            {
                charge.Driver.Wallet += charge.Value;
                _context.Charges.Remove(charge);
                await _context.SaveChangesAsync();
                return Ok("Charge approved and wallet updated.");
            }
            else if (dto.Action.ToLower() == "reject")
            {
                _context.Charges.Remove(charge);
                await _context.SaveChangesAsync();
                return Ok("Charge rejected and removed.");
            }
            else
            {
                return BadRequest("Invalid action. Use 'approve' or 'reject'.");
            }
        }
    }
}

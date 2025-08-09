using Microsoft.AspNetCore.Mvc;
using Snap.Core.Entities;
using Snap.APIs.DTOs;
using Snap.Repository.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
                UserId = driver.UserId
            };
            return Ok(dto);
        }
    }
}

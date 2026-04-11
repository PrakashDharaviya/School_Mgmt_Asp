using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SchoolEduERP.Services;

namespace SchoolEduERP.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AcademicYearApiController : ControllerBase
    {
        private readonly IAcademicYearService _academicYearService;

        public AcademicYearApiController(IAcademicYearService academicYearService)
        {
            _academicYearService = academicYearService;
        }

        // GET: api/AcademicYearApi/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveYear()
        {
            var year = await _academicYearService.GetActiveYearAsync();
            if (year == null) 
            {
                return NotFound(new { message = "No active academic year found." });
            }

            return Ok(new { year.Id, year.Name, year.StartDate, year.EndDate, year.IsActive });
        }

        // GET: api/AcademicYearApi
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllYears()
        {
            var years = await _academicYearService.GetAllYearsAsync();
            var result = years.Select(y => new { y.Id, y.Name, y.StartDate, y.EndDate, y.IsActive });
            return Ok(result);
        }
    }
}
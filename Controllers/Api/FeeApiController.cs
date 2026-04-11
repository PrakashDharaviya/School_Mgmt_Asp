using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SchoolEduERP.Data.Repositories;

namespace SchoolEduERP.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class FeeApiController : ControllerBase
    {
        private readonly IFeeHeadRepository _feeRepo;

        public FeeApiController(IFeeHeadRepository feeRepo)
        {
            _feeRepo = feeRepo;
        }

        // GET: api/FeeApi/academic-year/1
        [HttpGet("academic-year/{yearId}")]
        public async Task<IActionResult> GetFeesByYear(int yearId)
        {
            var fees = await _feeRepo.GetByAcademicYearAsync(yearId);
            var result = fees.Select(f => new 
            { 
                f.Id, f.Name, f.Amount, f.ApplicableClass, f.DueDate
            });
            return Ok(result);
        }

        // GET: api/FeeApi/overdue
        [HttpGet("overdue")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetOverdueFees()
        {
            var fees = await _feeRepo.GetOverdueFeeHeadsAsync();
            var result = fees.Select(f => new 
            { 
                f.Id, f.Name, f.Amount, f.ApplicableClass, f.DueDate
            });
            return Ok(result);
        }
    }
}
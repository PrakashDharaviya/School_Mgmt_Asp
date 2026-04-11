using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SchoolEduERP.Data.Repositories;
using SchoolEduERP.Models.Domain;

namespace SchoolEduERP.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    // This attribute enforces that the API can ONLY be accessed via a valid JWT token
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class StudentApiController : ControllerBase
    {
        private readonly IStudentRepository _studentRepo;

        public StudentApiController(IStudentRepository studentRepo)
        {
            _studentRepo = studentRepo;
        }

        // GET: api/StudentApi
        [HttpGet]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetActiveStudents()
        {
            var students = await _studentRepo.GetActiveStudentsAsync();
            var result = students.Select(s => new
            {
                s.Id,
                s.AdmissionNumber,
                s.FirstName,
                s.LastName,
                s.DateOfBirth,
                s.Gender,
                s.IsActive
            });
            
            return Ok(result);
        }

        // GET: api/StudentApi/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(int id)
        {
            var student = await _studentRepo.GetByIdAsync(id);
            if (student == null)
            {
                return NotFound(new { message = $"Student with Id {id} not found." });
            }

            return Ok(new
            {
                student.Id,
                student.AdmissionNumber,
                student.FirstName,
                student.LastName,
                student.Email
            });
        }
    }
}
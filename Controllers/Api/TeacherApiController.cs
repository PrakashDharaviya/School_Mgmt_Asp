using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SchoolEduERP.Data.Repositories;

namespace SchoolEduERP.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TeacherApiController : ControllerBase
    {
        private readonly ITeacherRepository _teacherRepo;

        public TeacherApiController(ITeacherRepository teacherRepo)
        {
            _teacherRepo = teacherRepo;
        }

        // GET: api/TeacherApi
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetActiveTeachers()
        {
            var teachers = await _teacherRepo.GetActiveTeachersAsync();
            var result = teachers.Select(t => new
            {
                t.Id,
                t.EmployeeId,
                t.FirstName,
                t.LastName,
                t.Email,
                t.Phone,
                t.Specialization,
                t.IsActive
            });
            
            return Ok(result);
        }

        // GET: api/TeacherApi/5
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Teacher")]
        public async Task<IActionResult> GetTeacherById(int id)
        {
            var teacher = await _teacherRepo.GetByIdAsync(id);
            if (teacher == null)
            {
                return NotFound(new { message = $"Teacher with Id {id} not found." });
            }

            return Ok(new
            {
                teacher.Id,
                teacher.EmployeeId,
                teacher.FirstName,
                teacher.LastName,
                teacher.Email,
                teacher.Phone,
                teacher.Qualification,
                teacher.JoiningDate
            });
        }
    }
}
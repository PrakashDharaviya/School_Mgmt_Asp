using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SchoolEduERP.Data;
using SchoolEduERP.Data.Repositories;
using SchoolEduERP.Middleware;
using SchoolEduERP.Models.Domain;
using SchoolEduERP.Services;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ----- Database -----
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ----- Identity -----
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ----- Authorization Policies -----
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminAccess", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TeacherAccess", policy => policy.RequireRole("Admin", "Teacher"));
    options.AddPolicy("StudentAccess", policy => policy.RequireRole("Admin", "Teacher", "Student"));
});

// ----- Repositories -----
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IFeeHeadRepository, FeeHeadRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ----- Services -----
builder.Services.AddScoped<IAcademicYearService, AcademicYearService>();
builder.Services.AddScoped<IGpaCalculatorService, GpaCalculatorService>();
builder.Services.AddScoped<IFeeReminderService, FeeReminderService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddHostedService<FeeReminderBackgroundService>();

// ----- MVC -----
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ----- Apply EF Migrations + Seed default users at startup -----
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context     = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        // 1. Ensure roles exist
        foreach (var role in new[] { "Admin", "Teacher", "Student" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // 2. Default Admin
        if (await userManager.FindByEmailAsync("admin@schoolerp.com") == null)
        {
            var admin = new ApplicationUser
            {
                UserName     = "admin@schoolerp.com",
                Email        = "admin@schoolerp.com",
                FullName     = "System Administrator",
                RoleType     = "Admin",
                EmailConfirmed  = true,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            };
            var res = await userManager.CreateAsync(admin, "Admin@123");
            if (res.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
        }

        // ══════════ 3. SEED DATA (runs only when tables are empty) ══════════

        // ── Academic Years ──
        if (!context.AcademicYears.Any())
        {
            context.AcademicYears.AddRange(
                new AcademicYear { Name = "2024-25", StartDate = new DateTime(2024, 4, 1), EndDate = new DateTime(2025, 3, 31), IsActive = false },
                new AcademicYear { Name = "2025-26", StartDate = new DateTime(2025, 4, 1), EndDate = new DateTime(2026, 3, 31), IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        var activeYear = await context.AcademicYears.FirstAsync(a => a.IsActive);

        // ── Class Sections (Std 1-12, Section A; plus Std 10 B, 11 B, 12 B) ──
        if (!context.ClassSections.Any())
        {
            for (int i = 1; i <= 12; i++)
                context.ClassSections.Add(new ClassSection { ClassName = i.ToString(), Section = "A", Capacity = 40 });
            context.ClassSections.Add(new ClassSection { ClassName = "10", Section = "B", Capacity = 40 });
            context.ClassSections.Add(new ClassSection { ClassName = "11", Section = "B", Capacity = 40 });
            context.ClassSections.Add(new ClassSection { ClassName = "12", Section = "B", Capacity = 40 });
            await context.SaveChangesAsync();
        }

        // ── Courses / Streams ──
        if (!context.Courses.Any())
        {
            context.Courses.AddRange(
                new Course { Name = "Science",        Code = "SCI",   Credits = 5 },
                new Course { Name = "Commerce",       Code = "COM",   Credits = 4 },
                new Course { Name = "Arts",            Code = "ART",   Credits = 4 },
                new Course { Name = "Computer Science", Code = "CS",   Credits = 3 },
                new Course { Name = "Physical Education", Code = "PE", Credits = 2 }
            );
            await context.SaveChangesAsync();
        }

        // ── Teachers (10 teachers) ──
        if (!context.Teachers.Any())
        {
            var teacherData = new[]
            {
                ("Ramesh",   "Patel",     "ramesh.patel@schoolerp.com",    "EMP001", "Mathematics",       "M.Sc. Mathematics"),
                ("Sunita",   "Sharma",    "sunita.sharma@schoolerp.com",   "EMP002", "English",           "M.A. English"),
                ("Vikram",   "Singh",     "vikram.singh@schoolerp.com",    "EMP003", "Science",           "M.Sc. Physics"),
                ("Priya",    "Desai",     "priya.desai@schoolerp.com",     "EMP004", "Hindi",             "M.A. Hindi"),
                ("Amit",     "Joshi",     "amit.joshi@schoolerp.com",      "EMP005", "Social Studies",    "M.A. History"),
                ("Neha",     "Gupta",     "neha.gupta@schoolerp.com",      "EMP006", "Computer Science",  "M.Tech CS"),
                ("Rajesh",   "Kumar",     "rajesh.kumar@schoolerp.com",    "EMP007", "Physical Education","B.P.Ed"),
                ("Kavita",   "Mehta",     "kavita.mehta@schoolerp.com",    "EMP008", "Chemistry",         "M.Sc. Chemistry"),
                ("Suresh",   "Yadav",     "suresh.yadav@schoolerp.com",    "EMP009", "Biology",           "M.Sc. Biology"),
                ("Anjali",   "Verma",     "anjali.verma@schoolerp.com",    "EMP010", "Economics",         "M.A. Economics"),
            };

            foreach (var (fn, ln, email, empId, spec, qual) in teacherData)
            {
                var pwd = "Teacher@123";
                var teacher = new Teacher
                {
                    EmployeeId     = empId,
                    FirstName      = fn,
                    LastName       = ln,
                    Email          = email,
                    Phone          = "98" + empId[^3..] + "00" + empId[^2..],
                    Specialization = spec,
                    Qualification  = qual,
                    JoiningDate    = new DateTime(2023, 6, 1).AddDays(Random.Shared.Next(0, 365)),
                    IsActive       = true,
                    Password       = pwd,
                    CreatedAt      = DateTime.UtcNow,
                    UpdatedAt      = DateTime.UtcNow
                };
                context.Teachers.Add(teacher);
                await context.SaveChangesAsync();

                // Create Identity login
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var appUser = new ApplicationUser
                    {
                        UserName       = email,
                        Email          = email,
                        FullName       = fn + " " + ln,
                        RoleType       = "Teacher",
                        EmailConfirmed = true,
                        CreatedAt      = DateTime.UtcNow,
                        UpdatedAt      = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(appUser, pwd);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(appUser, "Teacher");
                        teacher.UserId = appUser.Id;
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        // Assign teachers to courses
        var allTeachers = await context.Teachers.ToListAsync();
        var allCourses  = await context.Courses.ToListAsync();
        foreach (var course in allCourses.Where(c => c.TeacherId == null))
        {
            course.TeacherId = allTeachers[Random.Shared.Next(allTeachers.Count)].Id;
        }
        await context.SaveChangesAsync();

        // ── Subjects (across standards 1-12, 20+ subjects) ──
        if (!context.Subjects.Any())
        {
            var subjectList = new (int std, string name, string code)[]
            {
                (1, "Mathematics", "MATH1"), (1, "English", "ENG1"), (1, "Hindi", "HIN1"), (1, "EVS", "EVS1"),
                (5, "Mathematics", "MATH5"), (5, "English", "ENG5"), (5, "Hindi", "HIN5"), (5, "Science", "SCI5"), (5, "Social Studies", "SS5"),
                (8, "Mathematics", "MATH8"), (8, "English", "ENG8"), (8, "Science", "SCI8"), (8, "Social Studies", "SS8"), (8, "Hindi", "HIN8"),
                (10, "Mathematics", "MATH10"), (10, "English", "ENG10"), (10, "Science", "SCI10"), (10, "Social Studies", "SS10"), (10, "Hindi", "HIN10"),
                (11, "Physics", "PHY11"), (11, "Chemistry", "CHEM11"), (11, "Mathematics", "MATH11"), (11, "English", "ENG11"), (11, "Computer Science", "CS11"),
                (12, "Physics", "PHY12"), (12, "Chemistry", "CHEM12"), (12, "Mathematics", "MATH12"), (12, "English", "ENG12"), (12, "Biology", "BIO12"),
            };
            foreach (var (std, name, code) in subjectList)
            {
                var matchTeacher = allTeachers.FirstOrDefault(t => t.Specialization != null && name.Contains(t.Specialization, StringComparison.OrdinalIgnoreCase))
                                   ?? allTeachers[Random.Shared.Next(allTeachers.Count)];
                context.Subjects.Add(new Subject { Standard = std, Name = name, Code = code, TeacherId = matchTeacher.Id });
            }
            await context.SaveChangesAsync();
        }

        // ── Students (25 students) ──
        if (!context.Students.Any())
        {
            var studentData = new[]
            {
                ("Aarav",    "Sharma",     "M", "aarav.sharma@schoolerp.com",    "Rakesh Sharma",    "9876500001", 10, new DateTime(2010, 3, 15)),
                ("Vivaan",   "Patel",      "M", "vivaan.patel@schoolerp.com",    "Nilesh Patel",     "9876500002", 10, new DateTime(2010, 7, 22)),
                ("Aditya",   "Singh",      "M", "aditya.singh@schoolerp.com",    "Rajveer Singh",    "9876500003", 10, new DateTime(2010, 1, 8)),
                ("Diya",     "Gupta",      "F", "diya.gupta@schoolerp.com",      "Sunil Gupta",      "9876500004", 10, new DateTime(2010, 11, 30)),
                ("Ananya",   "Verma",      "F", "ananya.verma@schoolerp.com",    "Manoj Verma",      "9876500005", 10, new DateTime(2010, 5, 14)),
                ("Ishaan",   "Kumar",      "M", "ishaan.kumar@schoolerp.com",    "Arun Kumar",       "9876500006", 8,  new DateTime(2012, 8, 20)),
                ("Saanvi",   "Joshi",      "F", "saanvi.joshi@schoolerp.com",    "Deepak Joshi",     "9876500007", 8,  new DateTime(2012, 2, 10)),
                ("Arjun",    "Mehta",      "M", "arjun.mehta@schoolerp.com",     "Sanjay Mehta",     "9876500008", 8,  new DateTime(2012, 9, 5)),
                ("Myra",     "Desai",      "F", "myra.desai@schoolerp.com",      "Hiren Desai",      "9876500009", 8,  new DateTime(2012, 6, 18)),
                ("Reyansh",  "Yadav",      "M", "reyansh.yadav@schoolerp.com",   "Gopal Yadav",      "9876500010", 8,  new DateTime(2012, 4, 25)),
                ("Kabir",    "Reddy",      "M", "kabir.reddy@schoolerp.com",     "Venkat Reddy",     "9876500011", 5,  new DateTime(2015, 12, 3)),
                ("Anika",    "Nair",       "F", "anika.nair@schoolerp.com",      "Gopal Nair",       "9876500012", 5,  new DateTime(2015, 10, 7)),
                ("Vihaan",   "Chauhan",    "M", "vihaan.chauhan@schoolerp.com",  "Bharat Chauhan",   "9876500013", 5,  new DateTime(2015, 1, 19)),
                ("Riya",     "Thakur",     "F", "riya.thakur@schoolerp.com",     "Mahesh Thakur",    "9876500014", 5,  new DateTime(2015, 7, 28)),
                ("Dhruv",    "Mishra",     "M", "dhruv.mishra@schoolerp.com",    "Pankaj Mishra",    "9876500015", 5,  new DateTime(2015, 3, 11)),
                ("Kavya",    "Saxena",     "F", "kavya.saxena@schoolerp.com",    "Alok Saxena",      "9876500016", 11, new DateTime(2009, 8, 16)),
                ("Arnav",    "Tiwari",     "M", "arnav.tiwari@schoolerp.com",    "Ravi Tiwari",      "9876500017", 11, new DateTime(2009, 5, 2)),
                ("Shanvi",   "Agarwal",    "F", "shanvi.agarwal@schoolerp.com",  "Vikas Agarwal",    "9876500018", 11, new DateTime(2009, 11, 21)),
                ("Rohan",    "Dubey",      "M", "rohan.dubey@schoolerp.com",     "Sushil Dubey",     "9876500019", 12, new DateTime(2008, 4, 9)),
                ("Prisha",   "Pandey",     "F", "prisha.pandey@schoolerp.com",   "Rajesh Pandey",    "9876500020", 12, new DateTime(2008, 9, 14)),
                ("Advait",   "Bhatt",      "M", "advait.bhatt@schoolerp.com",    "Kiran Bhatt",      "9876500021", 12, new DateTime(2008, 2, 27)),
                ("Kiara",    "Kapoor",     "F", "kiara.kapoor@schoolerp.com",    "Anil Kapoor",      "9876500022", 1,  new DateTime(2019, 6, 10)),
                ("Atharv",   "Rawat",      "M", "atharv.rawat@schoolerp.com",    "Dinesh Rawat",     "9876500023", 1,  new DateTime(2019, 12, 1)),
                ("Sara",     "Iyer",       "F", "sara.iyer@schoolerp.com",       "Subramaniam Iyer", "9876500024", 1,  new DateTime(2019, 3, 8)),
                ("Dev",      "Malhotra",   "M", "dev.malhotra@schoolerp.com",    "Vikash Malhotra",  "9876500025", 1,  new DateTime(2019, 8, 30)),
            };

            var classSections = await context.ClassSections.ToListAsync();
            var sciCourse     = allCourses.First(c => c.Code == "SCI");
            var comCourse     = allCourses.First(c => c.Code == "COM");
            int admSeq = 1;

            foreach (var (fn, ln, gender, email, guardian, gPhone, std, dob) in studentData)
            {
                var pwd = "Student@123";
                var admNo = $"ADM-2025-{admSeq:D4}";
                var student = new Student
                {
                    AdmissionNumber = admNo,
                    FirstName       = fn,
                    LastName        = ln,
                    Email           = email,
                    Phone           = "97" + admSeq.ToString("D3") + "00" + admSeq.ToString("D2"),
                    DateOfBirth     = dob,
                    Gender          = gender,
                    Address         = $"{admSeq} Model Town, City",
                    GuardianName    = guardian,
                    GuardianPhone   = gPhone,
                    AdmissionDate   = new DateTime(2025, 4, 1),
                    IsActive        = true,
                    Password        = pwd,
                    CreatedAt       = DateTime.UtcNow,
                    UpdatedAt       = DateTime.UtcNow
                };

                context.Students.Add(student);
                await context.SaveChangesAsync();

                // Create Identity login
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var appUser = new ApplicationUser
                    {
                        UserName       = email,
                        Email          = email,
                        FullName       = fn + " " + ln,
                        RoleType       = "Student",
                        EmailConfirmed = true,
                        CreatedAt      = DateTime.UtcNow,
                        UpdatedAt      = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(appUser, pwd);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(appUser, "Student");
                        student.UserId = appUser.Id;
                        await context.SaveChangesAsync();
                    }
                }

                // Enroll in class
                var section = classSections.FirstOrDefault(c => c.ClassName == std.ToString() && c.Section == "A");
                if (section != null)
                {
                    int? courseId = null;
                    if (std >= 11) courseId = sciCourse.Id;  // 11-12 get Science by default
                    if (std == 12 && admSeq % 3 == 0) courseId = comCourse.Id; // some get Commerce

                    var maxRoll = context.Enrollments
                        .Where(e => e.ClassSectionId == section.Id && e.AcademicYearId == activeYear.Id)
                        .Select(e => (int?)e.RollNumber).Max() ?? 0;

                    context.Enrollments.Add(new Enrollment
                    {
                        StudentId      = student.Id,
                        ClassSectionId = section.Id,
                        AcademicYearId = activeYear.Id,
                        CourseId       = courseId,
                        RollNumber     = maxRoll + 1,
                        IsActive       = true
                    });
                    await context.SaveChangesAsync();
                }
                admSeq++;
            }
        }

        // ── Fee Heads (for active year) ──
        if (!context.FeeHeads.Any())
        {
            context.FeeHeads.AddRange(
                new FeeHead { Name = "Tuition Fee",       Amount = 15000, ApplicableClass = null, AcademicYearId = activeYear.Id, DueDate = new DateTime(2025, 7, 15), IsActive = true },
                new FeeHead { Name = "Lab Fee",           Amount = 3000,  ApplicableClass = "10", AcademicYearId = activeYear.Id, DueDate = new DateTime(2025, 7, 15), IsActive = true },
                new FeeHead { Name = "Library Fee",       Amount = 1500,  ApplicableClass = null, AcademicYearId = activeYear.Id, DueDate = new DateTime(2025, 8, 1),  IsActive = true },
                new FeeHead { Name = "Activity Fee",      Amount = 2000,  ApplicableClass = null, AcademicYearId = activeYear.Id, DueDate = new DateTime(2025, 6, 30), IsActive = true },
                new FeeHead { Name = "Exam Fee",          Amount = 2500,  ApplicableClass = null, AcademicYearId = activeYear.Id, DueDate = new DateTime(2025, 9, 1),  IsActive = true },
                new FeeHead { Name = "Computer Lab Fee",  Amount = 2000,  ApplicableClass = "11", AcademicYearId = activeYear.Id, DueDate = new DateTime(2025, 7, 15), IsActive = true },
                new FeeHead { Name = "Sports Fee",        Amount = 1000,  ApplicableClass = null, AcademicYearId = activeYear.Id, DueDate = new DateTime(2025, 8, 15), IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        // ── Fee Payments (some students have paid) ──
        if (!context.FeePayments.Any())
        {
            var students = await context.Students.ToListAsync();
            var feeHeads = await context.FeeHeads.Where(f => f.IsActive).ToListAsync();
            var tuitionFee = feeHeads.First(f => f.Name == "Tuition Fee");
            var activityFee = feeHeads.First(f => f.Name == "Activity Fee");

            // First 15 students paid Tuition Fee
            foreach (var s in students.Take(15))
            {
                context.FeePayments.Add(new FeePayment
                {
                    StudentId     = s.Id,
                    FeeHeadId     = tuitionFee.Id,
                    AmountPaid    = tuitionFee.Amount,
                    PaymentDate   = new DateTime(2025, 5, Random.Shared.Next(1, 28)),
                    PaymentMethod = Random.Shared.Next(2) == 0 ? "Cash" : "Online",
                    TransactionId = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    Status        = "Completed"
                });
            }
            // First 10 students paid Activity Fee
            foreach (var s in students.Take(10))
            {
                context.FeePayments.Add(new FeePayment
                {
                    StudentId     = s.Id,
                    FeeHeadId     = activityFee.Id,
                    AmountPaid    = activityFee.Amount,
                    PaymentDate   = new DateTime(2025, 6, Random.Shared.Next(1, 28)),
                    PaymentMethod = "Online",
                    TransactionId = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                    Status        = "Completed"
                });
            }
            await context.SaveChangesAsync();
        }

        // ── Exams ──
        if (!context.Exams.Any())
        {
            var courses = await context.Courses.ToListAsync();
            var sciCourse = courses.First(c => c.Code == "SCI");
            var comCourse = courses.First(c => c.Code == "COM");
            var csCourse  = courses.First(c => c.Code == "CS");
            var artCourse = courses.First(c => c.Code == "ART");

            var exams = new[]
            {
                new Exam { Name = "Unit Test 1",      CourseId = sciCourse.Id, ExamDate = new DateTime(2025, 6, 15), TotalMarks = 50,  Room = "101" },
                new Exam { Name = "Mid-Term Exam",     CourseId = sciCourse.Id, ExamDate = new DateTime(2025, 9, 10), TotalMarks = 100, Room = "Hall-A" },
                new Exam { Name = "Unit Test 2",      CourseId = comCourse.Id, ExamDate = new DateTime(2025, 8, 20), TotalMarks = 50,  Room = "102" },
                new Exam { Name = "Pre-Board Exam",    CourseId = sciCourse.Id, ExamDate = new DateTime(2025, 12, 1), TotalMarks = 100, Room = "Hall-B" },
                new Exam { Name = "Final Exam",        CourseId = sciCourse.Id, ExamDate = new DateTime(2026, 2, 15), TotalMarks = 100, Room = "Hall-A" },
                new Exam { Name = "Unit Test 1 - CS",  CourseId = csCourse.Id,  ExamDate = new DateTime(2025, 7, 10), TotalMarks = 50,  Room = "Lab-1" },
            };
            context.Exams.AddRange(exams);
            await context.SaveChangesAsync();

            // Add exam schedules
            foreach (var exam in exams)
            {
                context.ExamSchedules.Add(new ExamSchedule
                {
                    ExamId     = exam.Id,
                    StartTime  = exam.ExamDate.AddHours(9),
                    EndTime    = exam.ExamDate.AddHours(12),
                    Room       = exam.Room,
                    Invigilator = allTeachers[Random.Shared.Next(allTeachers.Count)].FirstName + " " + allTeachers[Random.Shared.Next(allTeachers.Count)].LastName
                });
            }
            await context.SaveChangesAsync();
        }

        // ── Marks ──
        if (!context.MarkEntries.Any())
        {
            var gpaService = services.GetRequiredService<IGpaCalculatorService>();
            var exams     = await context.Exams.ToListAsync();
            var students  = await context.Students.Where(s => s.IsActive).ToListAsync();
            var unitTest1 = exams.FirstOrDefault(e => e.Name == "Unit Test 1");

            if (unitTest1 != null)
            {
                foreach (var s in students.Take(20))
                {
                    var marksObt = (decimal)(Random.Shared.Next(15, 50));
                    var (grade, gp) = gpaService.GetGrade(marksObt, unitTest1.TotalMarks);
                    context.MarkEntries.Add(new MarkEntry
                    {
                        StudentId     = s.Id,
                        ExamId        = unitTest1.Id,
                        CourseId      = unitTest1.CourseId,
                        MarksObtained = marksObt,
                        GradePoint    = gp,
                        LetterGrade   = grade,
                        IsPublished   = true
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        // ── Attendance Records (last 20 school days for enrolled students) ──
        if (!context.AttendanceRecords.Any())
        {
            var enrollments = await context.Enrollments
                .Where(e => e.IsActive && e.AcademicYearId == activeYear.Id)
                .ToListAsync();

            var startDate = new DateTime(2025, 5, 1);
            int dayCount = 0;
            var currentDate = startDate;

            while (dayCount < 20)
            {
                if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    foreach (var enr in enrollments)
                    {
                        context.AttendanceRecords.Add(new AttendanceRecord
                        {
                            StudentId      = enr.StudentId,
                            ClassSectionId = enr.ClassSectionId,
                            Date           = currentDate,
                            IsPresent      = Random.Shared.Next(100) < 85,   // ~85% attendance
                            Remarks        = null
                        });
                    }
                    dayCount++;
                }
                currentDate = currentDate.AddDays(1);
            }
            await context.SaveChangesAsync();
        }

        // ── Salaries (for all 10 teachers, 2 months) ──
        if (!context.Salaries.Any())
        {
            var teachers = await context.Teachers.Where(t => t.IsActive).ToListAsync();
            var months = new[] { "May 2025", "June 2025" };
            foreach (var month in months)
            {
                foreach (var t in teachers)
                {
                    var basic = 35000 + Random.Shared.Next(0, 20000);
                    var allowance = 5000 + Random.Shared.Next(0, 5000);
                    var deduction = 2000 + Random.Shared.Next(0, 3000);
                    context.Salaries.Add(new Salary
                    {
                        TeacherId   = t.Id,
                        BasicSalary = basic,
                        Allowances  = allowance,
                        Deductions  = deduction,
                        NetSalary   = basic + allowance - deduction,
                        PaymentDate = month == "May 2025" ? new DateTime(2025, 5, 28) : new DateTime(2025, 6, 28),
                        Month       = month,
                        Status      = month == "May 2025" ? "Paid" : "Pending"
                    });
                }
            }
            await context.SaveChangesAsync();
        }

    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during startup seeding.");
    }
}

// ----- Pipeline -----
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

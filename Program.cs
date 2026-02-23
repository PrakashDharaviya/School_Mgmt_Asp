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

        // Teacher and Student accounts are created through the admin UI (Teacher Management & Admission forms).
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

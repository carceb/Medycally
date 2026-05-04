using Medycally.Core;
using Medycally.Core.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath        = "/Account/Login";
        options.LogoutPath       = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan   = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Data access
builder.Services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IClinicType, ClinicType>();
builder.Services.AddScoped<IClinic, Clinic>();
builder.Services.AddScoped<ISpecialty, Specialty>();
builder.Services.AddScoped<IDoctorSchedule, DoctorSchedule>();
builder.Services.AddScoped<IPatient, Patient>();
builder.Services.AddScoped<IReason, Reason>();
builder.Services.AddScoped<IGeography, Geography>();
builder.Services.AddScoped<ICommonData, CommonData>();
builder.Services.AddScoped<IAppointment, Appointment>();
builder.Services.AddScoped<IAppointmentQuery, AppointmentQuery>();
builder.Services.AddScoped<ISecurityUser, SecurityUser>();
builder.Services.AddScoped<ISecurityModule, SecurityModule>();
builder.Services.AddScoped<IDoctor, Doctor>();
builder.Services.AddScoped<IAdminUser, AdminUser>();
builder.Services.AddScoped<ISecurityRole, SecurityRole>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMedicalAttention, MedicalAttention>();
builder.Services.AddScoped<IPatientHistory, PatientHistory>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "patient",
    pattern: "Patient/{action=Index}/{id?}",
    defaults: new { controller = "Patient" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}")
    .WithStaticAssets();

app.Run();

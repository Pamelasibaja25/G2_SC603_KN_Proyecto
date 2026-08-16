using G2_SC603_KN_Proyecto.Models;
using G2_SC603_KN_Proyecto.Services.Wod;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DbOrionFitContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(9, 4, 0)),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }
    ));

// Servicios de negocio de WOD
builder.Services.AddScoped<IWodConsultaService, WodConsultaService>();
builder.Services.AddScoped<IWodEliminacionService, WodEliminacionService>();
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews(options =>
{
    // Exige el token anti-CSRF en toda accion POST/PUT/DELETE/PATCH de la
    // app, sin tener que agregar [ValidateAntiForgeryToken] a mano en cada
    // controller (los forms de Razor ya generan el token solos).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;   // el JS del navegador no puede leer la cookie de sesión
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // exige HTTPS cuando la request ya vino por HTTPS (Railway siempre)
    options.Cookie.SameSite = SameSiteMode.Strict; // no se envía en requests iniciadas desde otro sitio
    options.IdleTimeout = TimeSpan.FromMinutes(30); // cierra sola tras 30 min de inactividad
});

var app = builder.Build();

app.UseSession();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
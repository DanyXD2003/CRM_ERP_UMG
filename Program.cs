using CRM_ERP_UMG.Data;
using CRM_ERP_UMG.Models;
using CRM_ERP_UMG.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var constructor = WebApplication.CreateBuilder(args);

var cadenaConexion = constructor.Configuration.GetConnectionString("ConexionPrincipal")
    ?? throw new InvalidOperationException("No se encontró la cadena de conexión ConexionPrincipal.");

var constructorPostgres = new NpgsqlDataSourceBuilder(cadenaConexion);
constructorPostgres.EnableDynamicJson();

var fuenteDatosPostgres = constructorPostgres.Build();

constructor.Services.AddDbContext<ContextoSistema>(opciones =>
{
    opciones.UseNpgsql(fuenteDatosPostgres);
});

constructor.Services
    .AddIdentity<UsuarioAplicacion, IdentityRole>(opciones =>
    {
        opciones.Password.RequireDigit = true;
        opciones.Password.RequireLowercase = true;
        opciones.Password.RequireUppercase = true;
        opciones.Password.RequireNonAlphanumeric = true;
        opciones.Password.RequiredLength = 6;
        opciones.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ContextoSistema>()
    .AddDefaultTokenProviders();

constructor.Services.ConfigureApplicationCookie(opciones =>
{
    opciones.LoginPath = "/Cuenta/Login";
    opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
});

constructor.Services.AddControllersWithViews();
constructor.Services.AddScoped<ServicioFormulas>();

var aplicacion = constructor.Build();

if (!aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseExceptionHandler("/Cuenta/Error");
    aplicacion.UseHsts();
}

using (var alcance = aplicacion.Services.CreateScope())
{
    var servicios = alcance.ServiceProvider;
    var contexto = servicios.GetRequiredService<ContextoSistema>();

    await contexto.Database.MigrateAsync();
    await SembradorDatos.CargarDatosIniciales(servicios);
}

aplicacion.UseHttpsRedirection();
aplicacion.UseStaticFiles();

aplicacion.UseRouting();

aplicacion.UseAuthentication();
aplicacion.UseAuthorization();

aplicacion.MapControllerRoute(
    name: "rutaPrincipal",
    pattern: "{controller=Modulos}/{action=Index}/{id?}");

aplicacion.Run();

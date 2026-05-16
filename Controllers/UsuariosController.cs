using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_ERP_UMG.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<UsuarioAplicacion> administradorUsuarios;
        private readonly RoleManager<IdentityRole> administradorRoles;

        public UsuariosController(
            UserManager<UsuarioAplicacion> administradorUsuarios,
            RoleManager<IdentityRole> administradorRoles)
        {
            this.administradorUsuarios = administradorUsuarios;
            this.administradorRoles = administradorRoles;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await administradorUsuarios.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var modelo = new List<UsuarioListadoVista>();

            foreach (var usuario in usuarios)
            {
                var roles = await administradorUsuarios.GetRolesAsync(usuario);

                modelo.Add(new UsuarioListadoVista
                {
                    Id = usuario.Id,
                    NombreCompleto = usuario.NombreCompleto,
                    Email = usuario.Email ?? "",
                    Roles = roles.ToList()
                });
            }

            return View(modelo);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View(new CrearUsuarioVista
            {
                RolesDisponibles = administradorRoles.Roles.Select(r => r.Name!).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CrearUsuarioVista modelo)
        {
            modelo.RolesDisponibles = administradorRoles.Roles.Select(r => r.Name!).ToList();

            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var usuario = new UsuarioAplicacion
            {
                UserName = modelo.Email,
                Email = modelo.Email,
                NombreCompleto = modelo.NombreCompleto,
                EmailConfirmed = true
            };

            var resultado = await administradorUsuarios.CreateAsync(usuario, modelo.Password);

            if (resultado.Succeeded)
            {
                await administradorUsuarios.AddToRoleAsync(usuario, modelo.Rol);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarRol(string usuarioId, string nuevoRol)
        {
            var usuario = await administradorUsuarios.FindByIdAsync(usuarioId);

            if (usuario == null)
            {
                return NotFound();
            }

            var rolesActuales = await administradorUsuarios.GetRolesAsync(usuario);

            await administradorUsuarios.RemoveFromRolesAsync(usuario, rolesActuales);
            await administradorUsuarios.AddToRoleAsync(usuario, nuevoRol);

            return RedirectToAction(nameof(Index));
        }
    }
}

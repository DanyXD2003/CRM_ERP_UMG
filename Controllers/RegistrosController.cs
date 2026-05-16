using CRM_ERP_UMG.Data;
using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_ERP_UMG.Controllers
{
    [Authorize]
    public class RegistrosController : Controller
    {
        private readonly ContextoSistema contexto;
        private readonly UserManager<UsuarioAplicacion> administradorUsuarios;

        public RegistrosController(
            ContextoSistema contexto,
            UserManager<UsuarioAplicacion> administradorUsuarios)
        {
            this.contexto = contexto;
            this.administradorUsuarios = administradorUsuarios;
        }

        public async Task<IActionResult> Index(int moduloId)
        {
            var modulo = await contexto.ModulosDinamicos.FindAsync(moduloId);

            if (modulo == null)
            {
                return NotFound();
            }

            var registros = await contexto.RegistrosDinamicos
                .Where(r => r.ModuloDinamicoId == moduloId)
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

            ViewBag.Modulo = modulo;
            return View(registros);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> Crear(int moduloId)
        {
            var modulo = await contexto.ModulosDinamicos.FindAsync(moduloId);

            if (modulo == null)
            {
                return NotFound();
            }

            ViewBag.Modulo = modulo;
            return View();
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int moduloId, IFormCollection formulario)
        {
            var modulo = await contexto.ModulosDinamicos.FindAsync(moduloId);

            if (modulo == null)
            {
                return NotFound();
            }

            var datos = new Dictionary<string, string>();

            foreach (var campo in modulo.EsquemaCampos)
            {
                datos[campo.Key] = formulario[campo.Key].ToString();
            }

            var usuario = await administradorUsuarios.GetUserAsync(User);

            var registro = new RegistroDinamico
            {
                ModuloDinamicoId = moduloId,
                Datos = datos,
                UsuarioCreacionId = usuario?.Id
            };

            contexto.RegistrosDinamicos.Add(registro);
            await contexto.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { moduloId });
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var registro = await contexto.RegistrosDinamicos
                .Include(r => r.Modulo)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registro == null || registro.Modulo == null)
            {
                return NotFound();
            }

            return View(registro);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, IFormCollection formulario)
        {
            var registro = await contexto.RegistrosDinamicos
                .Include(r => r.Modulo)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registro == null || registro.Modulo == null)
            {
                return NotFound();
            }

            foreach (var campo in registro.Modulo.EsquemaCampos)
            {
                registro.Datos[campo.Key] = formulario[campo.Key].ToString();
            }

            contexto.RegistrosDinamicos.Update(registro);
            await contexto.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { moduloId = registro.ModuloDinamicoId });
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var registro = await contexto.RegistrosDinamicos.FindAsync(id);

            if (registro == null)
            {
                return NotFound();
            }

            var moduloId = registro.ModuloDinamicoId;

            contexto.RegistrosDinamicos.Remove(registro);
            await contexto.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { moduloId });
        }
    }
}

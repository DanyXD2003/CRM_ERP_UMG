using CRM_ERP_UMG.Data;
using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CRM_ERP_UMG.Controllers
{
    [Authorize]
    public class ModulosController : Controller
    {
        private readonly ContextoSistema contexto;

        public ModulosController(ContextoSistema contexto)
        {
            this.contexto = contexto;
        }

        public async Task<IActionResult> Index()
        {
            var modulos = await contexto.ModulosDinamicos
                .OrderBy(m => m.NombreModulo)
                .ToListAsync();

            return View(modulos);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpGet]
        public IActionResult Crear()
        {
            return View();
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ModuloDinamico modulo)
        {
            if (!ModelState.IsValid)
            {
                return View(modulo);
            }

            modulo.EsquemaCampos ??= new Dictionary<string, CampoDinamico>();

            contexto.ModulosDinamicos.Add(modulo);
            await contexto.SaveChangesAsync();

            return RedirectToAction(nameof(Detalle), new { id = modulo.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var modulo = await contexto.ModulosDinamicos.FirstOrDefaultAsync(m => m.Id == id);

            if (modulo == null)
            {
                return NotFound();
            }

            modulo.EsquemaCampos ??= new Dictionary<string, CampoDinamico>();

            return View(modulo);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarCampo(
            int moduloId,
            string nombreCampo,
            string etiqueta,
            string tipoCampo,
            bool requerido = false)
        {
            var modulo = await contexto.ModulosDinamicos.FindAsync(moduloId);

            if (modulo == null)
            {
                return NotFound();
            }

            modulo.EsquemaCampos ??= new Dictionary<string, CampoDinamico>();

            var claveCampo = NormalizarClave(nombreCampo);

            if (string.IsNullOrWhiteSpace(claveCampo))
            {
                TempData["Error"] = "El nombre del campo no es válido.";
                return RedirectToAction(nameof(Detalle), new { id = moduloId });
            }

            if (modulo.EsquemaCampos.ContainsKey(claveCampo))
            {
                TempData["Error"] = "Ya existe un campo con ese nombre.";
                return RedirectToAction(nameof(Detalle), new { id = moduloId });
            }

            modulo.EsquemaCampos[claveCampo] = new CampoDinamico
            {
                NombreCampo = claveCampo,
                Etiqueta = string.IsNullOrWhiteSpace(etiqueta) ? claveCampo : etiqueta,
                TipoCampo = string.IsNullOrWhiteSpace(tipoCampo) ? "texto" : tipoCampo,
                Requerido = requerido
            };

            contexto.ModulosDinamicos.Update(modulo);

            var registros = await contexto.RegistrosDinamicos
                .Where(r => r.ModuloDinamicoId == moduloId)
                .ToListAsync();

            foreach (var registro in registros)
            {
                registro.Datos ??= new Dictionary<string, string>();

                if (!registro.Datos.ContainsKey(claveCampo))
                {
                    registro.Datos[claveCampo] = "";
                }

                contexto.RegistrosDinamicos.Update(registro);
            }

            await contexto.SaveChangesAsync();

            TempData["Ok"] = "Campo agregado correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = moduloId });
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarCampo(int moduloId, string nombreCampo)
        {
            var modulo = await contexto.ModulosDinamicos.FindAsync(moduloId);

            if (modulo == null)
            {
                return NotFound();
            }

            modulo.EsquemaCampos ??= new Dictionary<string, CampoDinamico>();

            if (modulo.EsquemaCampos.ContainsKey(nombreCampo))
            {
                modulo.EsquemaCampos.Remove(nombreCampo);
                contexto.ModulosDinamicos.Update(modulo);

                var registros = await contexto.RegistrosDinamicos
                    .Where(r => r.ModuloDinamicoId == moduloId)
                    .ToListAsync();

                foreach (var registro in registros)
                {
                    registro.Datos ??= new Dictionary<string, string>();

                    if (registro.Datos.ContainsKey(nombreCampo))
                    {
                        registro.Datos.Remove(nombreCampo);
                    }

                    contexto.RegistrosDinamicos.Update(registro);
                }

                await contexto.SaveChangesAsync();
            }

            TempData["Ok"] = "Campo eliminado correctamente.";
            return RedirectToAction(nameof(Detalle), new { id = moduloId });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarModulo(int id)
        {
            var modulo = await contexto.ModulosDinamicos.FindAsync(id);

            if (modulo == null)
            {
                return NotFound();
            }

            contexto.ModulosDinamicos.Remove(modulo);
            await contexto.SaveChangesAsync();

            TempData["Ok"] = "Módulo eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private string NormalizarClave(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return "";
            }

            var resultado = texto.Trim().ToLowerInvariant();
            resultado = resultado.Replace(" ", "_");
            resultado = Regex.Replace(resultado, @"[^a-zA-Z0-9_]", "");

            return resultado;
        }
    }
}

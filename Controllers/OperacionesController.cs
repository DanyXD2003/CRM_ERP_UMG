using CRM_ERP_UMG.Data;
using CRM_ERP_UMG.Models;
using CRM_ERP_UMG.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CRM_ERP_UMG.Controllers
{
    [Authorize]
    public class OperacionesController : Controller
    {
        private readonly ContextoSistema contexto;
        private readonly ServicioFormulas servicioFormulas;

        public OperacionesController(
            ContextoSistema contexto,
            ServicioFormulas servicioFormulas)
        {
            this.contexto = contexto;
            this.servicioFormulas = servicioFormulas;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? moduloId)
        {
            var consulta = contexto.OperacionesDinamicas
                .Include(o => o.Modulo)
                .AsQueryable();

            if (moduloId.HasValue)
            {
                consulta = consulta.Where(o => o.ModuloDinamicoId == moduloId.Value);
            }

            var operaciones = await consulta
                .OrderByDescending(o => o.FechaCreacion)
                .ToListAsync();

            ViewBag.ModuloId = moduloId;
            return View(operaciones);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpGet]
        public async Task<IActionResult> Crear(int moduloId)
        {
            var modulo = await contexto.ModulosDinamicos.FirstOrDefaultAsync(m => m.Id == moduloId);

            if (modulo == null)
            {
                TempData["Error"] = $"No existe un módulo con Id {moduloId}.";
                return RedirectToAction("Index", "Modulos");
            }

            ViewBag.Modulo = modulo;
            return View();
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(int moduloId, IFormCollection formulario)
        {
            var modulo = await contexto.ModulosDinamicos.FirstOrDefaultAsync(m => m.Id == moduloId);

            if (modulo == null)
            {
                TempData["Error"] = $"No existe un módulo con Id {moduloId}.";
                return RedirectToAction("Index", "Modulos");
            }

            var nombreOperacion = formulario["nombreOperacion"].ToString();

            if (string.IsNullOrWhiteSpace(nombreOperacion))
            {
                ViewBag.Modulo = modulo;
                ModelState.AddModelError("", "Debe ingresar el nombre de la operación.");
                return View();
            }

            var columnasVisibles = formulario["columnasVisibles"].ToList();
            var nombresFormula = formulario["nombresFormula"].ToList();
            var expresionesFormula = formulario["expresionesFormula"].ToList();

            var formulas = new List<FormulaDinamica>();

            for (int indice = 0; indice < nombresFormula.Count; indice++)
            {
                var nombreResultado = NormalizarClave(nombresFormula[indice] ?? "");
                var expresion = expresionesFormula.ElementAtOrDefault(indice) ?? "";

                if (!string.IsNullOrWhiteSpace(nombreResultado) &&
                    !string.IsNullOrWhiteSpace(expresion))
                {
                    formulas.Add(new FormulaDinamica
                    {
                        NombreResultado = nombreResultado,
                        Expresion = expresion
                    });
                }
            }

            if (!formulas.Any())
            {
                ViewBag.Modulo = modulo;
                ModelState.AddModelError("", "Debe ingresar al menos una fórmula.");
                return View();
            }

            var operacion = new OperacionDinamica
            {
                ModuloDinamicoId = moduloId,
                NombreOperacion = nombreOperacion,
                ColumnasVisibles = columnasVisibles.Where(c => c != null).Select(c => c!).ToList(),
                Formulas = formulas
            };

            contexto.OperacionesDinamicas.Add(operacion);
            await contexto.SaveChangesAsync();

            return RedirectToAction(nameof(Resultado), new { id = operacion.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Resultado(int id)
        {
            var operacion = await contexto.OperacionesDinamicas
                .Include(o => o.Modulo)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (operacion == null || operacion.Modulo == null)
            {
                TempData["Error"] = "La operación no existe.";
                return RedirectToAction(nameof(Index));
            }

            var registros = await contexto.RegistrosDinamicos
                .Where(r => r.ModuloDinamicoId == operacion.ModuloDinamicoId)
                .OrderByDescending(r => r.FechaCreacion)
                .ToListAsync();

            var filasProcesadas = new List<Dictionary<string, object>>();

            foreach (var registro in registros)
            {
                var fila = new Dictionary<string, object>();
                var variables = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

                foreach (var dato in registro.Datos)
                {
                    if (decimal.TryParse(
                        dato.Value.Replace(",", "."),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var valorDecimal))
                    {
                        variables[dato.Key] = valorDecimal;
                    }
                }

                foreach (var columna in operacion.ColumnasVisibles)
                {
                    fila[columna] = registro.Datos.ContainsKey(columna)
                        ? registro.Datos[columna]
                        : "";
                }

                foreach (var formula in operacion.Formulas)
                {
                    try
                    {
                        var resultado = servicioFormulas.Evaluar(formula.Expresion, variables);
                        fila[formula.NombreResultado] = resultado;
                        variables[formula.NombreResultado] = resultado;
                    }
                    catch (Exception error)
                    {
                        fila[formula.NombreResultado] = $"Error: {error.Message}";
                    }
                }

                filasProcesadas.Add(fila);
            }

            ViewBag.Operacion = operacion;
            return View(filasProcesadas);
        }

        [Authorize(Roles = "Admin,Editor")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var operacion = await contexto.OperacionesDinamicas.FirstOrDefaultAsync(o => o.Id == id);

            if (operacion == null)
            {
                TempData["Error"] = "La operación no existe.";
                return RedirectToAction(nameof(Index));
            }

            contexto.OperacionesDinamicas.Remove(operacion);
            await contexto.SaveChangesAsync();

            TempData["Ok"] = "Operación eliminada correctamente.";
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

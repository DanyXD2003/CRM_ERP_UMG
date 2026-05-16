using CRM_ERP_UMG.Data;
using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_ERP_UMG.Controllers
{
    [Authorize(Roles = "Admin,Editor")]
    public class ProductosController : Controller
    {
        private readonly ContextoSistema contexto;

        public ProductosController(ContextoSistema contexto)
        {
            this.contexto = contexto;
        }

        public async Task<IActionResult> Index()
        {
            var productos = await contexto.Productos
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View(productos);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View(new Producto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Producto producto)
        {
            if (!ModelState.IsValid)
            {
                return View(producto);
            }

            contexto.Productos.Add(producto);
            await contexto.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var producto = await contexto.Productos.FindAsync(id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(Producto producto)
        {
            if (!ModelState.IsValid)
            {
                return View(producto);
            }

            contexto.Productos.Update(producto);
            await contexto.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}

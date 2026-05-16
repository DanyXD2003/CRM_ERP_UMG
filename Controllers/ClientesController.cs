using CRM_ERP_UMG.Data;
using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM_ERP_UMG.Controllers
{
    [Authorize(Roles = "Admin,Editor,Vendedor")]
    public class ClientesController : Controller
    {
        private readonly ContextoSistema contexto;

        public ClientesController(ContextoSistema contexto)
        {
            this.contexto = contexto;
        }

        public async Task<IActionResult> Index()
        {
            var clientes = await contexto.Clientes
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(clientes);
        }

        [HttpGet]
        public IActionResult Crear()
        {
            return View(new Cliente());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Cliente cliente)
        {
            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            contexto.Clientes.Add(cliente);
            await contexto.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}

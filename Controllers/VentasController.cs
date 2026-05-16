using CRM_ERP_UMG.Data;
using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace CRM_ERP_UMG.Controllers
{
    [Authorize(Roles = "Admin,Editor,Vendedor")]
    public class VentasController : Controller
    {
        private readonly ContextoSistema contexto;
        private readonly UserManager<UsuarioAplicacion> administradorUsuarios;

        public VentasController(
            ContextoSistema contexto,
            UserManager<UsuarioAplicacion> administradorUsuarios)
        {
            this.contexto = contexto;
            this.administradorUsuarios = administradorUsuarios;
        }

        public async Task<IActionResult> Index()
        {
            var ventas = await contexto.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();

            return View(ventas);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            ViewBag.Clientes = await contexto.Clientes
                .Where(c => c.Activo)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            ViewBag.Productos = await contexto.Productos
                .Where(p => p.Activo && p.Existencia > 0)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(IFormCollection formulario)
        {
            var usuario = await administradorUsuarios.GetUserAsync(User);

            if (usuario == null)
            {
                return Unauthorized();
            }

            int? clienteId = null;

            if (int.TryParse(formulario["clienteId"].ToString(), out var clienteIdConvertido))
            {
                clienteId = clienteIdConvertido;
            }

            var descuento = ConvertirDecimal(formulario["descuento"].ToString());
            var productosId = formulario["productoId"].ToList();
            var cantidades = formulario["cantidad"].ToList();

            if (!productosId.Any())
            {
                TempData["Error"] = "Debe agregar al menos un producto a la venta.";
                return RedirectToAction(nameof(Crear));
            }

            using var transaccion = await contexto.Database.BeginTransactionAsync();

            try
            {
                var venta = new Venta
                {
                    NumeroVenta = GenerarNumeroVenta(),
                    ClienteId = clienteId,
                    UsuarioId = usuario.Id,
                    FechaVenta = DateTime.UtcNow,
                    Descuento = descuento,
                    Estado = "Emitida"
                };

                contexto.Ventas.Add(venta);
                await contexto.SaveChangesAsync();

                decimal subtotalVenta = 0;

                for (int indice = 0; indice < productosId.Count; indice++)
                {
                    if (!int.TryParse(productosId[indice], out var productoId))
                    {
                        continue;
                    }

                    var cantidad = ConvertirDecimal(cantidades.ElementAtOrDefault(indice) ?? "0");

                    if (cantidad <= 0)
                    {
                        continue;
                    }

                    var producto = await contexto.Productos.FirstOrDefaultAsync(p => p.Id == productoId);

                    if (producto == null)
                    {
                        throw new Exception("Producto no encontrado.");
                    }

                    if (producto.Existencia < cantidad)
                    {
                        throw new Exception($"No hay suficiente existencia para {producto.Nombre}. Stock actual: {producto.Existencia}");
                    }

                    var subtotalLinea = producto.PrecioVenta * cantidad;

                    var detalle = new DetalleVenta
                    {
                        VentaId = venta.Id,
                        ProductoId = producto.Id,
                        Cantidad = cantidad,
                        PrecioUnitario = producto.PrecioVenta,
                        Subtotal = subtotalLinea
                    };

                    producto.Existencia -= cantidad;
                    subtotalVenta += subtotalLinea;

                    contexto.DetallesVenta.Add(detalle);
                    contexto.Productos.Update(producto);
                }

                if (subtotalVenta <= 0)
                {
                    throw new Exception("La venta no tiene productos válidos.");
                }

                venta.Subtotal = subtotalVenta;
                venta.Impuesto = subtotalVenta * 0.12m;
                venta.Total = venta.Subtotal + venta.Impuesto - venta.Descuento;

                contexto.Ventas.Update(venta);
                await contexto.SaveChangesAsync();

                await transaccion.CommitAsync();

                return RedirectToAction(nameof(Detalle), new { id = venta.Id });
            }
            catch (Exception error)
            {
                await transaccion.RollbackAsync();

                TempData["Error"] = error.Message;
                return RedirectToAction(nameof(Crear));
            }
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var venta = await contexto.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Usuario)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
            {
                return NotFound();
            }

            return View(venta);
        }

        private string GenerarNumeroVenta()
        {
            return $"V-{DateTime.Now:yyyyMMddHHmmss}";
        }

        private decimal ConvertirDecimal(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return 0;
            }

            valor = valor.Replace(",", ".");

            decimal.TryParse(
                valor,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var resultado
            );

            return resultado;
        }
    }
}

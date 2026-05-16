using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRM_ERP_UMG.Models
{
    public class UsuarioAplicacion : IdentityUser
    {
        [Required]
        public string NombreCompleto { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    public class ModuloDinamico
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string NombreModulo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public Dictionary<string, CampoDinamico> EsquemaCampos { get; set; } = new();

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public List<RegistroDinamico> Registros { get; set; } = new();

        public List<OperacionDinamica> Operaciones { get; set; } = new();
    }

    public class CampoDinamico
    {
        public string NombreCampo { get; set; } = string.Empty;

        public string Etiqueta { get; set; } = string.Empty;

        public string TipoCampo { get; set; } = "texto";

        public bool Requerido { get; set; }
    }

    public class RegistroDinamico
    {
        [Key]
        public int Id { get; set; }

        public int ModuloDinamicoId { get; set; }

        public ModuloDinamico? Modulo { get; set; }

        [Column(TypeName = "jsonb")]
        public Dictionary<string, string> Datos { get; set; } = new();

        public string? UsuarioCreacionId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    public class OperacionDinamica
    {
        [Key]
        public int Id { get; set; }

        public int ModuloDinamicoId { get; set; }

        public ModuloDinamico? Modulo { get; set; }

        [Required]
        public string NombreOperacion { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public List<string> ColumnasVisibles { get; set; } = new();

        [Column(TypeName = "jsonb")]
        public List<FormulaDinamica> Formulas { get; set; } = new();

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }

    public class FormulaDinamica
    {
        public string NombreResultado { get; set; } = string.Empty;

        public string Expresion { get; set; } = string.Empty;
    }

    public class Cliente
    {
        [Key]
        public int Id { get; set; }

        public string Nit { get; set; } = "CF";

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public List<Venta> Ventas { get; set; } = new();
    }

    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        public string Nombre { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        public decimal PrecioVenta { get; set; }

        public decimal Existencia { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public List<DetalleVenta> DetallesVenta { get; set; } = new();
    }

    public class Venta
    {
        [Key]
        public int Id { get; set; }

        public string NumeroVenta { get; set; } = string.Empty;

        public int? ClienteId { get; set; }

        public Cliente? Cliente { get; set; }

        public string UsuarioId { get; set; } = string.Empty;

        public UsuarioAplicacion? Usuario { get; set; }

        public DateTime FechaVenta { get; set; } = DateTime.UtcNow;

        public decimal Subtotal { get; set; }

        public decimal Impuesto { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public string Estado { get; set; } = "Emitida";

        public List<DetalleVenta> Detalles { get; set; } = new();
    }

    public class DetalleVenta
    {
        [Key]
        public int Id { get; set; }

        public int VentaId { get; set; }

        public Venta? Venta { get; set; }

        public int ProductoId { get; set; }

        public Producto? Producto { get; set; }

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }
    }
}

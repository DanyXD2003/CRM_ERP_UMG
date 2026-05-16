using CRM_ERP_UMG.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CRM_ERP_UMG.Data
{
    public class ContextoSistema : IdentityDbContext<UsuarioAplicacion>
    {
        public ContextoSistema(DbContextOptions<ContextoSistema> opciones) : base(opciones)
        {
        }

        public DbSet<ModuloDinamico> ModulosDinamicos { get; set; }

        public DbSet<RegistroDinamico> RegistrosDinamicos { get; set; }

        public DbSet<OperacionDinamica> OperacionesDinamicas { get; set; }

        public DbSet<Cliente> Clientes { get; set; }

        public DbSet<Producto> Productos { get; set; }

        public DbSet<Venta> Ventas { get; set; }

        public DbSet<DetalleVenta> DetallesVenta { get; set; }

        protected override void OnModelCreating(ModelBuilder constructorModelo)
        {
            base.OnModelCreating(constructorModelo);

            constructorModelo.Entity<ModuloDinamico>()
                .Property(m => m.EsquemaCampos)
                .HasColumnType("jsonb");

            constructorModelo.Entity<RegistroDinamico>()
                .Property(r => r.Datos)
                .HasColumnType("jsonb");

            constructorModelo.Entity<OperacionDinamica>()
                .Property(o => o.ColumnasVisibles)
                .HasColumnType("jsonb");

            constructorModelo.Entity<OperacionDinamica>()
                .Property(o => o.Formulas)
                .HasColumnType("jsonb");

            constructorModelo.Entity<RegistroDinamico>()
                .HasOne(r => r.Modulo)
                .WithMany(m => m.Registros)
                .HasForeignKey(r => r.ModuloDinamicoId)
                .OnDelete(DeleteBehavior.Cascade);

            constructorModelo.Entity<OperacionDinamica>()
                .HasOne(o => o.Modulo)
                .WithMany(m => m.Operaciones)
                .HasForeignKey(o => o.ModuloDinamicoId)
                .OnDelete(DeleteBehavior.Cascade);

            constructorModelo.Entity<Producto>()
                .Property(p => p.PrecioVenta)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<Producto>()
                .Property(p => p.Existencia)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<Venta>()
                .Property(v => v.Subtotal)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<Venta>()
                .Property(v => v.Impuesto)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<Venta>()
                .Property(v => v.Descuento)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<Venta>()
                .Property(v => v.Total)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<DetalleVenta>()
                .Property(d => d.Cantidad)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<DetalleVenta>()
                .Property(d => d.PrecioUnitario)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<DetalleVenta>()
                .Property(d => d.Subtotal)
                .HasColumnType("numeric(18,2)");

            constructorModelo.Entity<Venta>()
                .HasOne(v => v.Cliente)
                .WithMany(c => c.Ventas)
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            constructorModelo.Entity<Venta>()
                .HasOne(v => v.Usuario)
                .WithMany()
                .HasForeignKey(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            constructorModelo.Entity<DetalleVenta>()
                .HasOne(d => d.Venta)
                .WithMany(v => v.Detalles)
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.Cascade);

            constructorModelo.Entity<DetalleVenta>()
                .HasOne(d => d.Producto)
                .WithMany(p => p.DetallesVenta)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

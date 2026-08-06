using Microsoft.EntityFrameworkCore;
using Helpdesk.Api.Models;
namespace Helpdesk.Api.Data;
public class HelpdeskDbContext : DbContext
{
    public HelpdeskDbContext(DbContextOptions<HelpdeskDbContext> options) : base(options) {}
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TicketDetalle> TicketDetalles => Set<TicketDetalle>();
    public DbSet<TicketAdjunto> TicketAdjuntos => Set<TicketAdjunto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<ConfiguracionEmpresa> ConfiguracionesEmpresa => Set<ConfiguracionEmpresa>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TicketDetalle>()
            .HasOne(td => td.Usuario)
            .WithMany(u => u.TicketDetalles)
            .HasForeignKey(td => td.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.AgenteAsignado)
            .WithMany()
            .HasForeignKey(t => t.AgenteAsignadoId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Usuario)
            .WithMany(u => u.Tickets)
            .HasForeignKey(t => t.UsuarioCreo)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketAdjunto>()
            .HasOne(ta => ta.Ticket)
            .WithMany()
            .HasForeignKey(ta => ta.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketAdjunto>()
            .HasOne(ta => ta.SubidoPor)
            .WithMany()
            .HasForeignKey(ta => ta.SubidoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketAdjunto>()
            .HasOne(ta => ta.TicketDetalle)
            .WithMany()
            .HasForeignKey(ta => ta.TicketDetalleId)
            .OnDelete(DeleteBehavior.Restrict);


        //Limites de caracteres
        modelBuilder.Entity<TicketAdjunto>()
            .Property(ta => ta.NombreOriginal)
            .HasMaxLength(255);

        modelBuilder.Entity<TicketAdjunto>()
            .Property(ta => ta.NombreAlmacenado) 
            .HasMaxLength(100);

        modelBuilder.Entity<TicketAdjunto>()
            .Property(ta => ta.ContentType)
            .HasMaxLength(100);

        //Indices 
        modelBuilder.Entity<TicketAdjunto>()
            .HasIndex(ta => ta.NombreAlmacenado)
            .IsUnique();

        modelBuilder.Entity<Categoria>()
            .HasIndex(c => c.Nombre)
            .IsUnique();

        //Ticket a categoria
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Categoria)
            .WithMany()
            .HasForeignKey(t => t.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        //Establezco los limites de propiedades
        modelBuilder.Entity<Categoria>()
            .Property(c => c.Nombre)
            .HasMaxLength (90);

        modelBuilder.Entity<Categoria>()
            .Property(c => c.Descripcion)
            .HasMaxLength(200);

        modelBuilder.Entity<Categoria>()
            .Property(c => c.Icono)
            .HasMaxLength(200);

        modelBuilder.Entity<ConfiguracionEmpresa>()
            .Property(e => e.NombreEmpresa)
            .HasMaxLength(120);

        //seed para datos
        modelBuilder.Entity<Categoria>()
            .HasData(new Categoria
            {
                Id = 1,
                Nombre = "Otra cosa",
                Icono = "HelpOutline",
                EsDelSistema = true,
                Activa = true,
                PrioridadSugerida = PrioridadTicket.Media,
                DiasVencimiento = null
            });

        modelBuilder.Entity<ConfiguracionEmpresa>()
            .HasData(new ConfiguracionEmpresa
            {
                Id = 1,
                NombreEmpresa = "Mi empresa",
                UsarFechaVencimiento = false
            });

        
    }
}
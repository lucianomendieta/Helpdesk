namespace Helpdesk.Api.Models;

public class Ticket
{
    public int Id {get; set;}
    public string Titulo {get; set;} = "";
    public string Descripcion {get; set;} = "";
    public DateTimeOffset FechaCreacion {get; set;} = DateTimeOffset.UtcNow;
    public int UsuarioCreo {get; set;}
    public Usuario? AgenteAsignado {get; set;}
    public int? AgenteAsignadoId {get; set;}
    public EstadoTicket Estado {get; set;} = EstadoTicket.Abierto;
    public Usuario? Usuario {get; set;}
    public PrioridadTicket Prioridad { get; set; } = PrioridadTicket.Media;
    public Categoria? Categoria {get; set;}
    public int? CategoriaId {get; set;}
    public DateTimeOffset? FechaVencimiento { get; set; }
    public DateTimeOffset? FechaCierre { get; set; }
}
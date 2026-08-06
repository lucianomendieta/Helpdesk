namespace Helpdesk.Web.Dtos;

using Helpdesk.Web.Models;

public class TicketDto
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTimeOffset FechaCreacion { get; set; }
    public int UsuarioCreo { get; set; }
    public int? AgenteAsignadoId { get; set; }
    public EstadoTicket Estado { get; set; }
    public string NombreCreador { get; set; } = string.Empty;
    public string? NombreAgente { get; set; }
    public PrioridadTicket Prioridad { get; set; }
    
    //Categorias
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public string? CategoriaIcono { get; set; }
    
    //Vencimiento
    public DateTimeOffset? FechaVencimiento { get; set; }

}

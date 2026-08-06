using Helpdesk.Web.Models;

namespace Helpdesk.Web.Dtos;

public class CrearTicketDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public PrioridadTicket Prioridad { get; set; } = PrioridadTicket.Media;
    public int CategoriaId { get; set; }
    public DateTimeOffset? FechaVencimiento { get; set; }
}

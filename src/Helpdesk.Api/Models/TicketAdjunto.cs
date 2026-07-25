namespace Helpdesk.Api.Models;

public class TicketAdjunto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int? TicketDetalleId { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string NombreAlmacenado { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanioBytes { get; set; }
    public int SubidoPorId { get; set; }
    public DateTimeOffset FechaSubida { get; set; } = DateTimeOffset.UtcNow;
    public TipoAdjunto Tipo {  get; set; }

    //FKs
    public Usuario? SubidoPor { get; set; }
    public Ticket? Ticket { get; set; }
    public TicketDetalle? TicketDetalle { get; set; }

}

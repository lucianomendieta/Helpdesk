namespace Helpdesk.Web.Dtos;

using Helpdesk.Web.Models;

public class AdjuntoResponseDto
{
    public int Id { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanioBytes { get; set; }
    public TipoAdjunto Tipo { get; set; }
    public DateTimeOffset FechaSubida { get; set; }
    public string SubidoPor { get; set; } = string.Empty;
}

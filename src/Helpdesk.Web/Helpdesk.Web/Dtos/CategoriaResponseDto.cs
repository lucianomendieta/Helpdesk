using Helpdesk.Web.Models;

namespace Helpdesk.Web.Dtos;

public class CategoriaResponseDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Icono { get; set; } = string.Empty;
    public PrioridadTicket PrioridadSugerida { get; set; }
    public bool Activa { get; set; }
    public bool EsDelSistema { get; set; }
    public int? DiasVencimiento { get; set; }
}

namespace Helpdesk.Api.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre{ get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Icono { get; set; } = string.Empty;
    public PrioridadTicket PrioridadSugerida { get; set; } = PrioridadTicket.Media;
    public bool Activa { get; set; } = true;
    public bool EsDelSistema { get; set; } = false;

}

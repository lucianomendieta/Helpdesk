namespace Helpdesk.Api.Models;

public class ConfiguracionEmpresa
{
    public int Id { get; set; }
    public string NombreEmpresa { get; set; } = string.Empty;
    public bool UsarFechaVencimiento { get; set; }

}

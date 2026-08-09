namespace Helpdesk.Web.Dtos;

public class ReporteResponseDto
{
    public List<ReporteCategoriaDto> PorCategoria  { get; set; }
    public List<ReporteMesDto> PorMes  { get; set; }
    public ReporteTiempoResolucionDto TiempoResolucion { get; set; }
}
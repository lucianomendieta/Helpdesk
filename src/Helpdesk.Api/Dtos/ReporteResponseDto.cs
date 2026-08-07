namespace Helpdesk.Api.Dtos;

public record ReporteResponseDto(
    List<ReporteCategoriaDto> PorCategoria,
    List<ReporteMesDto> PorMes,
    ReporteTiempoResolucionDto TiempoResolucion
    );
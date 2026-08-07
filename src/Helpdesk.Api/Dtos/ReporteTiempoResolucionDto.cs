namespace Helpdesk.Api.Dtos;

public record ReporteTiempoResolucionDto(
    double? PromedioDias,
    int CantidadResueltos
    );
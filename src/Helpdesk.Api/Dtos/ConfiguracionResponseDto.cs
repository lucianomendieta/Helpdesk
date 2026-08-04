namespace Helpdesk.Api.Dtos;

public record ConfiguracionResponseDto(
    int Id,
    string NombreEmpresa,
    bool UsarFechaVencimiento
    );
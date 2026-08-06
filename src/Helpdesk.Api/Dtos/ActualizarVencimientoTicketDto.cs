namespace Helpdesk.Api.Dtos;

public record ActualizarVencimientoTicketDto(
    DateTimeOffset? FechaVencimiento
    );
using Helpdesk.Api.Models;

namespace Helpdesk.Api.Dtos;

public record AdjuntoResponseDto
(
    int Id,
    string NombreOriginal,
    string ContentType,
    long TamanioBytes,
    TipoAdjunto Tipo,
    DateTimeOffset FechaSubida,
    string SubidoPor
    );

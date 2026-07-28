
using System.Security.Claims;

using Helpdesk.Api.Almacenamiento;
using Helpdesk.Api.Authorization;
using Helpdesk.Api.Data;
using Helpdesk.Api.Models;
using Helpdesk.Api.Dtos;

using Microsoft.EntityFrameworkCore;


namespace Helpdesk.Api.Endpoints;

public static class AdjuntoEndpoints
{
    public static IEndpointRouteBuilder MapAdjuntoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tickets/{ticketId}/adjuntos")
            .WithTags("Adjuntos")
            .RequireAuthorization();

        group.MapPost("/", PostAdjunto).DisableAntiforgery();
        group.MapGet("/", GetAdjuntos);
        group.MapGet("{adjuntoId}/contenido", GetContenido);

        return app;
    }


    //Extensiones permitidas
    private static readonly Dictionary<string, (TipoAdjunto Tipo, string ContentType) > ExtensionesPermitidas = new()
    {
        { ".jpeg", (TipoAdjunto.Imagen, "image/jpeg" ) },
        { ".jpg", (TipoAdjunto.Imagen, "image/jpeg" ) },
        { ".png", (TipoAdjunto.Imagen, "image/png" ) },
        { ".webp", (TipoAdjunto.Imagen, "image/webp" ) },
        {  ".pdf", (TipoAdjunto.Documento, "application/pdf")  }
    };

    //Armo el diccionario para cantidad de documentos
    private static readonly Dictionary<TipoAdjunto, (long MaxSize, int MaxCantidad)> LimitesPorTipo = new()
    {
        {TipoAdjunto.Imagen, (5*1024*1024, 7) },
        {TipoAdjunto.Documento, (10*1024*1024, 2) }
    };

    //Firmas de archivos
    private static readonly byte[] FirmaPng = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] FirmaWebpRiff = { 0x52, 0x49, 0x46, 0x46 };
    private static readonly byte[] FirmaWebp = { 0x57, 0x45, 0x42, 0x50 };
    private static readonly byte[] FirmaPdf = { 0x25, 0x50, 0x44, 0x46 };



    //Subir un archivo
    private static async Task<IResult> PostAdjunto(int ticketId, IFormFile archivo, HelpdeskDbContext contexto, HttpContext http, IAlmacenamientoAdjuntos almacenamiento)
    {
        //Valido los permisos del usuario logueado
        #region Validacion de permisos
        //Leo los roles
        var rol = http.User.FindFirstValue(ClaimTypes.Role);

        //Existe el ticket? Si no, 404
        var ticket = await contexto.Tickets.FindAsync(ticketId);
        if (ticket == null) { return Results.NotFound(); }

        //Si el usuario no se pudo leer, 401
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuario == null) { return Results.Unauthorized(); }

        var usuarioInt = int.Parse(usuario);

        //Si el usuario no es participante, esta prohibido que suba archivos
        if (!TicketPermisos.EsParticipante(ticket, rol, usuarioInt)) { return Results.Forbid(); }
        #endregion

        //Valido el archivo a subir
        #region Validacion de archivo
        //Chequeo si el archivo esta vacio
        if (archivo.Length == 0) { return Results.BadRequest("El archivo esta vacio."); }

        //Extensiones permitidas, si no esta en el dictionary, lanzo badrequest
        var archivoExtension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.TryGetValue(archivoExtension, out var datos))
        {
            return Results.BadRequest("El tipo de archivo no esta permitido.");
        }
        var limites = LimitesPorTipo[datos.Tipo]; // Uso variable para no repetir la declaracin del diccionario

        //Chequeo si excede el tamaño maximo
        if (archivo.Length > limites.MaxSize) { return Results.BadRequest($"El archivo es demasiado grande, no debe de sobrepasar los {(limites.MaxSize) / (1024*1024)}MB"); }

        

        //Creo el buffer, Jpeg = 3B, PNG = 8B, Webp = 12B
        var bytes = new byte[12];

        //Abro el stream y leo los doce primeros bytes
        using var stream = archivo.OpenReadStream();
        var leidos = await stream.ReadAtLeastAsync(bytes, bytes.Length, false);

        //Si los bytes leidos son menos de 12:
        if (leidos < bytes.Length){ return Results.BadRequest("El archivo es demasiado pequeño o esta corrupto"); }

        //Si la firma del archivo no coincide, no se sube.
        if (!FirmaValida(bytes, archivoExtension)) { return Results.BadRequest("El contenido del archivo no coincide con el tipo declarado"); }

        //Verifico que el cupo de archivos no se ha llenado
        var cantidadActual = await contexto.TicketAdjuntos.CountAsync(a => a.TicketId == ticketId && a.Tipo == datos.Tipo);
        if (cantidadActual >= limites.MaxCantidad)
        {
            return Results.BadRequest($"Ya alcanzaste el maximo de {limites.MaxCantidad} archivos para tu ticket.");
        }
        #endregion

        
        //Creo nuevo nombre para el archivo
        var nombreNuevo = Guid.NewGuid().ToString() + archivoExtension;

        //Guardo en disco
        using var streamSaveDisk = archivo.OpenReadStream();
        await almacenamiento.GuardarArchivoAsync(streamSaveDisk, nombreNuevo);

        //Creo el nuevo objeto
        var nuevoAdjunto = new TicketAdjunto
        {
            TicketId = ticketId,
            NombreOriginal = archivo.FileName,
            NombreAlmacenado = nombreNuevo,
            ContentType = datos.ContentType,
            TamanioBytes = archivo.Length,
            SubidoPorId = usuarioInt,
            Tipo = datos.Tipo
        };

        contexto.TicketAdjuntos.Add(nuevoAdjunto);
        await contexto.SaveChangesAsync();

        return Results.Created($"/tickets/{ticketId}/adjuntos/{nuevoAdjunto.Id}/contenido",
            await contexto.TicketAdjuntos.Where(a => a.Id == nuevoAdjunto.Id)
            .Select(a => new AdjuntoResponseDto(
                a.Id,
                a.NombreOriginal,
                a.ContentType,
                a.TamanioBytes,
                a.Tipo,
                a.FechaSubida,
                a.SubidoPor.NombrePila + " " + a.SubidoPor.ApellidoPila
                )).FirstAsync()
            );

    }


    private static async Task<IResult> GetAdjuntos(int ticketId, HelpdeskDbContext contexto, HttpContext http)
    {
        //Declaro las variables a usar
        var ticket = await contexto.Tickets.FindAsync(ticketId);
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rol = http.User.FindFirstValue(ClaimTypes.Role);

        // Si el ticket, usuario y no es participante, no puede hacer nada
        #region
        if (ticket is null) { return Results.NotFound(); }

        if (usuario is null) { return Results.Unauthorized(); }

        var usuarioInt = int.Parse(usuario);

        if (!TicketPermisos.EsParticipante(ticket, rol, usuarioInt))
        {
            return Results.Forbid();
        }
        #endregion

        var query = contexto.TicketAdjuntos.Where(a => a.TicketId == ticketId);

        //Si es cliente, si el comentario es interno no puede ver el ticket
        if (rol == "Cliente")
        {
            query = query.Where(a => a.TicketDetalleId == null || !a.TicketDetalle!.EsInterno); //Doble ! en ticketdetalle por null forgiving y que no sea interno
        }

        //Ordeno por fecha subida y luego por id
        query = query.OrderBy(a => a.FechaSubida).ThenBy(a => a.Id);

        //Devuelvo el response ya filtrado
        return Results.Ok(await query
            .Select(a => new AdjuntoResponseDto (
                a.Id,
                a.NombreOriginal,
                a.ContentType,
                a.TamanioBytes,
                a.Tipo,
                a.FechaSubida,
                a.SubidoPor.NombrePila + " " + a.SubidoPor.ApellidoPila
            )).ToListAsync());
    }

    private static async Task<IResult> GetContenido(int adjuntoId, int ticketId, HelpdeskDbContext contexto, HttpContext http, IAlmacenamientoAdjuntos almacenamiento)
    {
        //Declaro las variables a usar
        var ticket = await contexto.Tickets.FindAsync(ticketId);
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rol = http.User.FindFirstValue(ClaimTypes.Role);

        // Si el ticket, usuario y no es participante, no puede hacer nada
        #region
        if (ticket is null) { return Results.NotFound(); }

        if (usuario is null) { return Results.Unauthorized(); }

        var usuarioInt = int.Parse(usuario);

        if (!TicketPermisos.EsParticipante(ticket, rol, usuarioInt))
        {
            return Results.Forbid();
        }
        #endregion

        //busco el adjunto con doble condicion
        var adjunto = await contexto.TicketAdjuntos
            .Where(a => a.Id == adjuntoId && a.TicketId == ticketId) //Si el adjunto es el mismo que el que pido, mismo caso ticket
            .Include(a => a.TicketDetalle)
            .FirstOrDefaultAsync();

        if (adjunto is null) { return Results.NotFound(); }

        if ((rol == "Cliente") && (adjunto.TicketDetalleId != null && adjunto.TicketDetalle!.EsInterno)) // Si es cliente y el adjunto tiene detalle y sea interno, no ve el adjunto
        {
                return Results.NotFound();
        }

        //guardo el stream y verifico si es null
        var stream = await almacenamiento.AbrirArchivoAsync(adjunto.NombreAlmacenado);
        if (stream is null) { return Results.NotFound(); }

        //Creo un nullable para distinguir imagenes de documentos
        string? nombre = adjunto.Tipo == TipoAdjunto.Imagen ? null : adjunto.NombreOriginal;
        //Devuelvo el archivo con el overload de stream
        return Results.File(stream, adjunto.ContentType, fileDownloadName: nombre);
    }


    //Valido si la firma del archivo coincide con su extension
    private static bool FirmaValida(byte[] cabecera, string extension)
    {
        switch (extension)
        {
            //jpg y jpeg comparten firma, los demas se comparan contra el array declarado.
            case ".jpg":
            case ".jpeg":
                return cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF;

            case ".png":
                return cabecera.AsSpan(0, 8).SequenceEqual(FirmaPng);

            case ".webp":
                return cabecera.AsSpan(0, 4).SequenceEqual(FirmaWebpRiff) && cabecera.AsSpan(8, 4).SequenceEqual(FirmaWebp);

            case ".pdf":
                return cabecera.AsSpan(0, 4).SequenceEqual(FirmaPdf);

            default: return false;

        }
    }
}

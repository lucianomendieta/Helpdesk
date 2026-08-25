using Microsoft.EntityFrameworkCore;
using Helpdesk.Api.Data;
using Helpdesk.Api.Dtos;
using Helpdesk.Api.Models;
using System.Security.Claims;
using Helpdesk.Api.Authorization;

namespace Helpdesk.Api.Endpoints;

public static class TicketDetalleEndpoints
{
    public static IEndpointRouteBuilder MapTicketDetalleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tickets/{ticketId}/detalles")
                        .WithTags("TicketDetalles")
                        .RequireAuthorization();

        group.MapGet("/", GetDetalles);
        group.MapPost("/", PostDetalle);

        return app;
    }

    //Todos los detalles de un ticket si existen.
    private static async Task<IResult> GetDetalles(int ticketId, HelpdeskDbContext contexto, HttpContext http)
    {
        var ticket = await contexto.Tickets.FindAsync(ticketId);
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rol = http.User.FindFirstValue(ClaimTypes.Role);

        //Verificamos si hay ticket y usuario autorizados
        if (ticket is null)
        {
            return Results.NotFound();
        }
        if (usuario is null)
        {
            return Results.Unauthorized();
        }
        
        var usuarioInt = int.Parse(usuario); //Parse para el id usuario

        //Si no puede ver el detalle, forbid
        if (!TicketPermisos.PuedeVer(ticket, rol, usuarioInt))
        {
            return Results.Forbid();
        }

        var ticketDetalle =  contexto.TicketDetalles.Where(d => d.TicketId == ticketId);
        if (rol == "Cliente")
        {
            ticketDetalle = ticketDetalle.Where(d => !d.EsInterno);
        }
        return Results.Ok(await ticketDetalle
            .OrderBy(td => td.UltimaRevision)
            .ThenBy(td => td.Id)
            .Select(td => new TicketDetalleResponseDto(
                    td.Id,
                    td.Comentario,
                    td.Usuario.NombrePila + " " + td.Usuario.ApellidoPila,
                    td.Usuario.Rol,
                    td.UltimaRevision,
                    td.EsInterno
                )).ToListAsync());
}

    //Crear un detalle
    private static async Task<IResult> PostDetalle(int ticketId, CrearTicketDetalleDto dto, HttpContext http, HelpdeskDbContext contexto)
    {
        //obtengo el tickety
        var ticket = await contexto.Tickets.FindAsync(ticketId);
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rol = http.User.FindFirstValue(ClaimTypes.Role);

        if (ticket is null) { return Results.NotFound(); }

        if (usuario is null){ return Results.Unauthorized(); }

        var usuarioInt = int.Parse(usuario);

        if (!TicketPermisos.EsParticipante(ticket, rol, usuarioInt))
        {
            return Results.Forbid();
        }


        TicketDetalle nuevo = new TicketDetalle
        {
            TicketId = ticketId,
            Comentario = dto.Comentario,
            UsuarioId = usuarioInt,
            EsInterno = (rol == "Cliente") ? false : dto.EsInterno
        };
        contexto.TicketDetalles.Add(nuevo);
        await contexto.SaveChangesAsync();
        return Results.Created($"/tickets/{ticketId}/detalles/{nuevo.Id}", 
            await contexto.TicketDetalles.Where(td => td.Id == nuevo.Id)
            .Select(td => new TicketDetalleResponseDto(
                td.Id,
                td.Comentario,
                td.Usuario.NombrePila + " " + td.Usuario.ApellidoPila,
                td.Usuario.Rol,
                td.UltimaRevision,
                td.EsInterno
                )).FirstAsync());
    }
}
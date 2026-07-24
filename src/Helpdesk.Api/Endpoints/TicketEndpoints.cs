using System.Security.Claims;

using Helpdesk.Api.Authorization;
using Helpdesk.Api.Data;
using Helpdesk.Api.Dtos;
using Helpdesk.Api.Models;

using Microsoft.EntityFrameworkCore;

using static System.Net.WebRequestMethods;

namespace Helpdesk.Api.Endpoints;

public static class TicketEndpoints
{
    public static IEndpointRouteBuilder MapTicketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tickets").WithTags("Tickets").RequireAuthorization();

        //Gets
        group.MapGet("/", GetTickets);
        group.MapGet("/{id}", GetTicketId);
        group.MapGet("/stats", GetStats);
        group.MapGet("/recents", GetRecents);
        //Posts
        group.MapPost("/", PostTicket);
        //Puts
        group.MapPut("/{id}", PutTicket);
        group.MapPut("/{ticketId}/assign", AssignTicket)
            .RequireAuthorization("SoloAdmins");
        group.MapPut("/{ticketId}/status", StatusTicket)
            .RequireAuthorization("SoloPersonal");
        group.MapPut("/{ticketId}/priority", ChangePriorityTicket)
            .RequireAuthorization("SoloPersonal");
        //Deletes
        group.MapDelete("/{id}", DeleteTicket)
            .RequireAuthorization("SoloAdmins");

        return app;
    }

    //Get TODOS los tickets
    private static async Task<IResult> GetTickets(HelpdeskDbContext contexto, HttpContext http)
    {
        var rol = http.User.FindFirstValue(ClaimTypes.Role);
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = contexto.Tickets.AsQueryable();

        if (usuario is null)
        {
            return Results.Unauthorized();
        }

        var usuarioInt = int.Parse(usuario);

        query = FiltrarPorRol(query, rol, usuarioInt);

        return Results.Ok(await query.Select(t => new TicketResponseDto(
                                                        t.Id,
                                                        t.Titulo,
                                                        t.Descripcion,
                                                        t.FechaCreacion,
                                                        t.Estado,
                                                        t.Usuario.NombrePila + " " + t.Usuario.ApellidoPila,
                                                        t.AgenteAsignado == null ? null : t.AgenteAsignado.NombrePila + " " + t.AgenteAsignado.ApellidoPila,
                                                        t.UsuarioCreo,
                                                        t.AgenteAsignadoId,
                                                        t.Prioridad)).ToListAsync());
    }

    //Get ticket por ID
    private static async Task<IResult> GetTicketId(int id, HelpdeskDbContext contexto, HttpContext http)
    {
        var ticket = await contexto.Tickets
            .Where(t => t.Id == id)
            .Select(t => new TicketResponseDto(
                t.Id,
                t.Titulo,
                t.Descripcion,
                t.FechaCreacion,
                t.Estado,
                t.Usuario.NombrePila + " " + t.Usuario.ApellidoPila,
                t.AgenteAsignado == null ? null : t.AgenteAsignado.NombrePila + " " + t.AgenteAsignado.ApellidoPila,
                t.UsuarioCreo,
                t.AgenteAsignadoId,
                t.Prioridad)).FirstOrDefaultAsync();
        var rol = http.User.FindFirstValue(ClaimTypes.Role);
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (ticket is null)
        {
            return Results.NotFound();
        }
        if (usuario is null)
        {
            return Results.Unauthorized();
        }
        //usuario se crea como string, parseo a int
        var usuarioInt = int.Parse(usuario);
        //Si es cliente y el id de usuario es distinto al que lo creo, forbid
        if (rol == "Cliente" && ticket.UsuarioCreo != usuarioInt)
        {
            return Results.Forbid();
        }
        //Si es agente o analista y el ticket no es el que tiene asignado, forbid
        else if ((rol == "Agente" || rol == "Analista") && ticket.AgenteAsignadoId != usuarioInt)
        {
            return Results.Forbid();
        }

        return Results.Ok(ticket);
    }

    //Crear ticket
    private static async Task<IResult> PostTicket(CrearTicketDto dto, HelpdeskDbContext contexto, HttpContext http)
    {
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (usuario is null)
        {
            return Results.Unauthorized();
        }


        Ticket nuevo = new Ticket
        {
            UsuarioCreo = int.Parse(usuario),
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            Prioridad = dto.Prioridad ?? PrioridadTicket.Media
        };

        contexto.Tickets.Add(nuevo);
        await contexto.SaveChangesAsync();
        return Results.Created($"/tickets/{nuevo.Id}", await contexto.Tickets
            .Where(t => t.Id == nuevo.Id)
            .Select(t => new TicketResponseDto(
                t.Id,
                t.Titulo,
                t.Descripcion,
                t.FechaCreacion,
                t.Estado,
                t.Usuario.NombrePila + " " + t.Usuario.ApellidoPila,
                t.AgenteAsignado == null ? null : t.AgenteAsignado.NombrePila + " " + t.AgenteAsignado.ApellidoPila,
                t.UsuarioCreo,
                t.AgenteAsignadoId,
                t.Prioridad
                )).FirstAsync());
    }

    //Modificar ticket (solo titulo y descripcion)
    private static async Task<IResult> PutTicket(int id, ActualizarTicketDto dto, HelpdeskDbContext contexto, HttpContext http)
    {
        var ticket = await contexto.Tickets.FindAsync(id);
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rol = http.User.FindFirstValue(ClaimTypes.Role);

        if (ticket is null)
        {
            return Results.NotFound();
        }
        if (usuario is null)
        {
            return Results.Unauthorized();
        }

        var usuarioInt = int.Parse(usuario); //Parse para el id usuario

        //Si no puede editar, forbid
        if (!TicketPermisos.EsParticipante(ticket, rol, usuarioInt))
        {
            return Results.Forbid();
        }

        ticket.Titulo = dto.Titulo;
        ticket.Descripcion = dto.Descripcion;

        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }

    //Borrar un ticket
    private static async Task<IResult> DeleteTicket(int id, HelpdeskDbContext contexto)
    {
        var ticket = await contexto.Tickets.FindAsync(id);
        if (ticket is null)
        {
            return Results.NotFound();
        }
        contexto.Tickets.Remove(ticket);
        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }

    //Asignar agente/analista a un ticket
    private static async Task<IResult> AssignTicket(int ticketId, HelpdeskDbContext contexto, AsignarTicketDto dto)
    {
        //Verifico si el ticket existe
        var ticket = await contexto.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return Results.NotFound();
        }

        //si el agente es null, desasignamos.
        if (dto.AgenteId is null)
        {
            ticket.AgenteAsignadoId = null;
            await contexto.SaveChangesAsync();
            return Results.NoContent();
        }

        //Verifico si el agente existe
        var agente = await contexto.Usuarios.FindAsync(dto.AgenteId);
        if (agente is null)
        {
            return Results.BadRequest("No se encontro el usuario a asignar");
        }

        //verifico si es cliente, sino no se puede asignar
        if (agente.Rol == RolUsuario.Agente || agente.Rol == RolUsuario.Analista)
        {
            ticket.AgenteAsignadoId = dto.AgenteId;
            await contexto.SaveChangesAsync();
            return Results.NoContent();
        }
        else
        {
            return Results.BadRequest("Solo puede asignar a un Agente o Analista.");
        }
    }

    //Cambiar de estados
    private static async Task<IResult> StatusTicket(int ticketId, HelpdeskDbContext contexto, ActualizarEstadoTicketDto dto, HttpContext http)
    {
        var rol = http.User.FindFirstValue(ClaimTypes.Role);
        //Verifico si el usuario a chequear existe
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuario is null)
        {
            return Results.Unauthorized();
        }
        //Parse a int
        var usuarioInt = int.Parse(usuario);
        //Verifico si el ticket existe
        var ticket = await contexto.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return Results.NotFound();
        }

        //Si no puede editar, forbid
        if (!TicketPermisos.PuedeGestionar(ticket, rol, usuarioInt))
        {
            return Results.Forbid();
        }
        //Verifico si tiene Estado
        if (dto.EstadoTicket is null)
        {
            return Results.BadRequest("Ingrese un estado para el ticket.");
        }
        ticket.Estado = dto.EstadoTicket.Value;

        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }


    //CambiarPrioridad
    private static async Task<IResult> ChangePriorityTicket(int ticketId, HelpdeskDbContext contexto, ActualizarPrioridadTicketDto dto, HttpContext http)
    {
        var rol = http.User.FindFirstValue(ClaimTypes.Role);
        //Verifico si el usuario a chequear existe
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (usuario is null)
        {
            return Results.Unauthorized();
        }
        //Parse a int
        var usuarioInt = int.Parse(usuario);
        //Verifico si el ticket existe
        var ticket = await contexto.Tickets.FindAsync(ticketId);
        if (ticket is null)
        {
            return Results.NotFound();
        }

        //Si no puede editar, forbid
        if (!TicketPermisos.PuedeGestionar(ticket, rol, usuarioInt))
        {
            return Results.Forbid();
        }
        //Verifico si tiene Prioridad
        if (dto.Prioridad is null)
        {
            return Results.BadRequest("Ingrese una prioridad para el ticket.");
        }
        ticket.Prioridad = dto.Prioridad.Value;

        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }

    //Obtengo el query de ticket
    private static IQueryable<Ticket> FiltrarPorRol(IQueryable<Ticket> query, string? rol, int usuarioId)
    {

        //Si es cliente, solo ve sus propios tickets, si es agente/analista, solo los que se les asigno. Admin y Gerencia ve todos.
        if (rol == "Cliente")
        {
            query = query.Where(t => t.UsuarioCreo == usuarioId);
        }
        else if (rol == "Agente" || rol == "Analista")
        {
            query = query.Where(t => t.AgenteAsignadoId == usuarioId);
        }
        return query;
    }

    //Obtener estadisticas
    private static async Task<IResult> GetStats(ClaimsPrincipal user, HelpdeskDbContext contexto)
    {
        var rol = user.FindFirstValue(ClaimTypes.Role);
        var usuario = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = contexto.Tickets.AsQueryable();

        if (usuario is null)
        {
            return Results.Unauthorized();
        }

        var usuarioInt = int.Parse(usuario);

        //agrupo por estado y cantidad
        var agrupados = await FiltrarPorRol(query, rol, usuarioInt)
            .GroupBy(t => t.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var total = agrupados.Sum(c => c.Cantidad);

        //cuanto los sin asignar
        var sinAsignar = await FiltrarPorRol(query, rol, usuarioInt)
            .Where(t => t.AgenteAsignadoId == null)
            .CountAsync();

        //funcion para contar los agrupados
        int ContarEstado(EstadoTicket estado) => agrupados.FirstOrDefault(t => t.Estado == estado)?.Cantidad ?? 0;

        return Results.Ok(new TicketStatsDto(
                total,
                ContarEstado(EstadoTicket.Abierto),
                ContarEstado(EstadoTicket.EnProgreso),
                ContarEstado(EstadoTicket.Hecho),
                ContarEstado(EstadoTicket.Pendiente),
                ContarEstado(EstadoTicket.Cerrado),
                sinAsignar
            ));
    }

    private static async Task<IResult> GetRecents(ClaimsPrincipal user, HelpdeskDbContext contexto)
    {
        //Obtengo informacion del usuario con los claims
        var rol = user.FindFirstValue(ClaimTypes.Role);
        var usuario = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var query = contexto.Tickets.AsQueryable();

        if (usuario is null)
        {
            return Results.Unauthorized();
        }

        var usuarioInt = int.Parse(usuario);

        //Armo el query
        var filtrados = await FiltrarPorRol(query, rol, usuarioInt)
            .OrderByDescending(t => t.FechaCreacion) //Los mas recientes
            .Take(5) //Solo 5
            .Select(t => new TicketResponseDto(
                    t.Id,
                    t.Titulo,
                    t.Descripcion,
                    t.FechaCreacion,
                    t.Estado,
                    t.Usuario.NombrePila + " " + t.Usuario.ApellidoPila,
                    t.AgenteAsignado == null ? null : t.AgenteAsignado.NombrePila + " " + t.AgenteAsignado.ApellidoPila,
                    t.UsuarioCreo,
                    t.AgenteAsignadoId,
                    t.Prioridad)).ToListAsync(); //Devuelvo el response en el formato conocido
        return Results.Ok(filtrados);
    }
}

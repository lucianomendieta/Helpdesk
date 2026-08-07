using System.Security.Claims;

using Helpdesk.Api.Authorization;
using Helpdesk.Api.Data;
using Helpdesk.Api.Dtos;
using Helpdesk.Api.Models;
using Microsoft.EntityFrameworkCore;

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
        group.MapGet("/reportes", GetReportes)
            .RequireAuthorization("SoloAdmins");
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
        group.MapPut("/{ticketId}/categoria", ChangeCategoryTicket)
            .RequireAuthorization("SoloPersonal");
        group.MapPut("{ticketId}/vencimiento", PutFechaVencimiento)
            .RequireAuthorization("SoloPersonal");
        //Deletes
        group.MapDelete("/{id}", DeleteTicket)
            .RequireAuthorization("SoloAdmins");

        return app;
    }

    
    #region Gets
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
                                                        t.Prioridad,
                                                        t.CategoriaId,
                                                        t.Categoria == null ? null : t.Categoria.Nombre,
                                                        t.Categoria == null ? null : t.Categoria.Icono,
                                                        t.FechaVencimiento)).ToListAsync());
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
                t.Prioridad,
                t.CategoriaId,
                t.Categoria == null ? null : t.Categoria.Nombre,
                t.Categoria == null ? null : t.Categoria.Icono,
                t.FechaVencimiento)).FirstOrDefaultAsync();
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
        
        //cuento los que vencen hoy
        var hoy = DateTime.Now.Date;
        var vencenHoy = await FiltrarPorRol(query, rol, usuarioInt)
            .Where(t => t.FechaVencimiento != null)
            .Where(t => t.FechaVencimiento >= hoy && t.FechaVencimiento < hoy.AddDays(1))
            .Where(t => t.Estado != EstadoTicket.Hecho && t.Estado != EstadoTicket.Cerrado)
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
                sinAsignar,
                vencenHoy
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
                    t.Prioridad,
                    t.CategoriaId,
                    t.Categoria == null ? null : t.Categoria.Nombre,
                    t.Categoria == null ? null : t.Categoria.Icono,
                    t.FechaVencimiento)).ToListAsync(); //Devuelvo el response en el formato conocido
        return Results.Ok(filtrados);
    }
    
    
    //GET REPORTES
    private static async Task<IResult> GetReportes(HelpdeskDbContext contexto, DateTimeOffset? desde,
        DateTimeOffset? hasta)
    {
        //Seteo las variables si mandan null
        var hastaFinal = hasta ?? DateTimeOffset.UtcNow;
        var desdeFinal = desde ?? hastaFinal.AddMonths(-12);

        //Creo un queryable base para filtrar luego
        var creados = contexto.Tickets.Where(t => t.FechaCreacion >= desdeFinal && t.FechaCreacion <= hastaFinal);

        #region Categorias

        //Traigo de la base agrupando
        var categoriasRaw = await creados
            .GroupBy(t => t.Categoria == null ? null : t.Categoria.Nombre) //Agrupo por categorias
            .Select(g => new { g.Key, Cantidad = g.Count() }) //Selecciono y saco cantidades
            .ToListAsync() ;
        
        //Mapeo en la columna del dto, resolviendo null
        var porCategoria = categoriasRaw
            .Select(x => new ReporteCategoriaDto(x.Key ?? "Sin categoria", x.Cantidad))
            .ToList();

        #endregion

        #region Meses

        //SQL: agrupo y cuento, proyectando a tipo anonimo
        var mesesRaw = await creados
            .GroupBy(t => new { t.FechaCreacion.Year, t.FechaCreacion.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Cantidad = g.Count() })
            .ToListAsync();

        //En memoria: ordeno y mapeo al DTO
        var porMes = mesesRaw
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .Select(x => new ReporteMesDto(x.Year, x.Month, x.Cantidad))
            .ToList();

        #endregion
        
        //Tickets resueltos en el rango
        var resueltos = contexto.Tickets.Where(t => t.FechaCierre != null
                                                    && t.FechaCierre >= desdeFinal
                                                    && t.FechaCierre <= hastaFinal);
        var cantidadResueltos = await resueltos.CountAsync();
        
        //Promedio de tiempo por tickets
        double? promedioDias = cantidadResueltos == 0
            ? null
            : await resueltos.AverageAsync(t =>
                (double)EF.Functions.DateDiffDay(t.FechaCreacion, t.FechaCierre!.Value));

        //Devuelvo la lista
        return Results.Ok(new ReporteResponseDto(porCategoria, porMes,
            new ReporteTiempoResolucionDto(promedioDias, cantidadResueltos)));
    }

    #endregion

    //Crear ticket
    private static async Task<IResult> PostTicket(CrearTicketDto dto, HelpdeskDbContext contexto, HttpContext http)
    {
        var usuario = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var rol = http.User.FindFirstValue(ClaimTypes.Role);
        var esPersonal = rol is "Agente" or "Analista" or "Administrador" or "Gerente";
        
        if (usuario is null)
        {
            return Results.Unauthorized();
        }

        //Leo si esta habilitado la config
        var config = await contexto.ConfiguracionesEmpresa.FirstAsync();
        
        //Creo la variable para la iteracion
        Categoria? cat = null;
        
        //valido que haya venido categoria, que exista y que este activa
        if (dto.CategoriaId is not null)
        {
            cat = await contexto.Categorias.FirstOrDefaultAsync(c => c.Id == dto.CategoriaId && c.Activa);
            if (cat is null) { return Results.BadRequest("La categoria no existe o esta inactiva"); }
        }
        
        //Defino el automatico desde la categoria === Funcion helper
        DateTimeOffset? autoDesdeCategoria(DateTimeOffset fechacreacion, int? DiasVencimiento) 
            => DiasVencimiento is null ? null : fechacreacion.AddDays(DiasVencimiento.Value);

        Ticket nuevo = new()
        {
            UsuarioCreo = int.Parse(usuario),
            Titulo = dto.Titulo,
            Descripcion = dto.Descripcion,
            Prioridad = dto.Prioridad ?? PrioridadTicket.Media,
            CategoriaId = dto.CategoriaId,
        };
        
        //Si el toggle esta prendido agregamos fecha de vencimiento
        if (config.UsarFechaVencimiento)
        {
            nuevo.FechaVencimiento =
                (esPersonal ? dto.FechaVencimiento : null) ?? autoDesdeCategoria(nuevo.FechaCreacion, cat?.DiasVencimiento);
        }
        else
        {
            nuevo.FechaVencimiento = null;
        }

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
                t.Prioridad,
                t.CategoriaId,
                t.Categoria == null ? null : t.Categoria.Nombre,
                t.Categoria == null ? null : t.Categoria.Icono,
                t.FechaVencimiento
                )).FirstAsync());
    }

    #region Puts
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
        
        //verifico si se cerro el ticket
        //Si el nuevo es cerrado y el viejo no era cerrado
        if (dto.EstadoTicket == EstadoTicket.Cerrado && ticket.Estado != EstadoTicket.Cerrado) 
        {
            ticket.FechaCierre = DateTimeOffset.UtcNow;
        }
        //Si el viejo estaba cerrado y el nuevo no
        else if (ticket.Estado == EstadoTicket.Cerrado && dto.EstadoTicket != EstadoTicket.Cerrado)
        {
            ticket.FechaCierre = null; //Se reabre el ticket
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

    //Cambiar categoria de ticket
    private static async Task<IResult> ChangeCategoryTicket(int ticketId, HelpdeskDbContext contexto, ActualizarCategoriaTicketDto dto, HttpContext http)
    {
        #region Validacion del usuario y rol
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
        #endregion

        #region Validacion de categorias
        //Validamos que el usuario haya ingresado una categoria
        if (dto.CategoriaId is null) { return Results.BadRequest("Ingrese una categoria"); }
        //Validamos que la categoria este activa y exista
        //valido que haya venido categoria, que exista y que este activa
        bool ok = await contexto.Categorias.AnyAsync(c => c.Id == dto.CategoriaId && c.Activa);
        if (!ok) { return Results.BadRequest("La categoria no existe o esta inactiva"); }

        #endregion

        ticket.CategoriaId = dto.CategoriaId;

        //Guardo la actualizacion correcta
        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }


    private static async Task<IResult> PutFechaVencimiento(int ticketId, HelpdeskDbContext contexto,
        ActualizarVencimientoTicketDto dto, HttpContext http)
    {
        #region Validacion del usuario y rol
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
        #endregion

        //Leo si esta habilitado la config
        var config = await contexto.ConfiguracionesEmpresa.FirstAsync();

        if (config.UsarFechaVencimiento)
        {
            ticket.FechaVencimiento = dto.FechaVencimiento;  //Se permite null
        }
        else
        {
            return Results.BadRequest("No se esta utilizando la fecha de vencimiento");
        }
        
        //Guardo la actualizacion correcta
        await contexto.SaveChangesAsync();
        return Results.NoContent();
    }

    #endregion

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

}

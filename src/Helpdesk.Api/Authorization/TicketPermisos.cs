using Helpdesk.Api.Dtos;
using Helpdesk.Api.Models;

namespace Helpdesk.Api.Authorization;

public static class TicketPermisos
{
    public static bool EsParticipante(this Ticket ticket, string? rol, int usuarioId)
    {

        return 
            //Es admin o gerente:
            rol == "Administrador" || rol == "Gerente" ||
            //Es cliente y es su propio ticket
            (rol == "Cliente" && ticket.UsuarioCreo == usuarioId) ||
            //Es agente o analista y tiene asignado el ticket
            ((rol == "Agente" || rol == "Analista") && ticket.AgenteAsignadoId == usuarioId);
    }
    public static bool PuedeGestionar(this Ticket ticket, string? rol, int usuarioId)
    {
        return //Es admin o gerente:
            rol == "Administrador" || rol == "Gerente" ||
            //Es agente o analista y tiene asignado el ticket
            ((rol == "Agente" || rol == "Analista") && ticket.AgenteAsignadoId == usuarioId);
    }

    public static bool PuedeVer(this Ticket ticket, string? rol, int usuarioId)
    {
        return //Solo staff
            rol == "Administrador" || rol == "Gerente" || rol == "Agente" || rol == "Analista" ||
            //Cliente ve su propio ticket
            ((rol == "Cliente") && ticket.UsuarioCreo == usuarioId);
    }
    
    public static bool PuedeVer(this TicketResponseDto ticket, string? rol, int usuarioId)
    {
        return //Solo staff
            rol == "Administrador" || rol == "Gerente" || rol == "Agente" || rol == "Analista" ||
            //Cliente ve su propio ticket
            ((rol == "Cliente") && ticket.UsuarioCreo == usuarioId);
    }
}

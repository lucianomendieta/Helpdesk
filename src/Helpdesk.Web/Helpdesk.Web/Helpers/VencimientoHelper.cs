using Helpdesk.Web.Dtos;
using Helpdesk.Web.Models;

using MudBlazor;

namespace Helpdesk.Web.Helpers;

public static class VencimientoHelper
{
    private static DateTime hoy => DateTime.Now.Date;

    public static Color GetVencimientoColor(this TicketDto ticket) => ticket switch
    {
        { Estado: EstadoTicket.Cerrado or EstadoTicket.Hecho } => Color.Default,
        { FechaVencimiento: null } => Color.Default,
        _ when ticket.FechaVencimiento.Value.Date < hoy => Color.Error,
        _ when ticket.FechaVencimiento.Value.Date == hoy => Color.Warning,
        _ => Color.Default

    };

    public static string GetVencimientoTexto(this TicketDto ticket) => ticket switch
    {
        {FechaVencimiento: null} => "",
        _ when ticket.FechaVencimiento.Value.Date < hoy => "Vencido",
        _ when ticket.FechaVencimiento.Value.Date == hoy => "Vence hoy",
        _ => ticket.FechaVencimiento.Value.Date.ToString("dd MMM")
    };

}   
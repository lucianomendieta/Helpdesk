using Helpdesk.Web.Models;

using MudBlazor;

namespace Helpdesk.Web.Helpers;

public static class EstadoTicketColorHelper
{
    public static Color GetColor(this EstadoTicket ticket) => ticket switch
    {
        EstadoTicket.Abierto => Color.Info,
        EstadoTicket.Pendiente => Color.Default,
        EstadoTicket.EnProgreso => Color.Warning,
        EstadoTicket.Cerrado => Color.Error,
        EstadoTicket.Hecho => Color.Success,
        _ => default

    };
    
    public static string CssSufijo(this EstadoTicket estado) => estado switch
    {
        EstadoTicket.Abierto    => "nuevo",
        EstadoTicket.Pendiente  => "pendiente",
        EstadoTicket.EnProgreso => "enproceso",
        EstadoTicket.Hecho      => "hecho",
        EstadoTicket.Cerrado    => "cerrado",
        _ => "cerrado"   // ← agregá el discard: el switch del Dashboard no lo tenía y tira warning
    };
    
    public static string ChipStyle(this EstadoTicket estado,
        string padding = "5px 11px", string radius = "20px", string fontSize = "12px") =>
        $"background:var(--estado-{estado.CssSufijo()}-bg); " +
        $"color:var(--estado-{estado.CssSufijo()}-txt); " +
        $"padding:{padding}; border-radius:{radius}; font-size:{fontSize}; font-weight:600";
}

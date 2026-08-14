using MudBlazor;
using Helpdesk.Web.Models;

namespace Helpdesk.Web.Helpers;

public static class PrioridadHelper
{
    public static Color GetColor(this PrioridadTicket prioridadTicket) => prioridadTicket switch
    {
        PrioridadTicket.Baja => Color.Default,
        PrioridadTicket.Media => Color.Info,
        PrioridadTicket.Alta => Color.Warning,
        PrioridadTicket.Urgente => Color.Error,
        _ => default
    };

    public static string CssSufijo(this PrioridadTicket prioridad) => prioridad.ToString().ToLowerInvariant();
    
    public static string ChipStyle(this PrioridadTicket prioridad,
        string padding = "5px 11px", string radius = "20px", string fontSize = "12px") =>
        $"background:var(--prioridad-{prioridad.CssSufijo()}-bg); " +
        $"color:var(--prioridad-{prioridad.CssSufijo()}-txt); " +
        $"padding:{padding}; border-radius:{radius}; font-size:{fontSize}; font-weight:600";

    public static string TextColor(this PrioridadTicket prioridad) => $"var(--prioridad-{prioridad.CssSufijo()}-txt)";
}

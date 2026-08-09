using System.Diagnostics;
using System.Net.Http.Json;

using Helpdesk.Web.Dtos;

namespace Helpdesk.Web.Services;

public interface IReportesService
{
    Task<ReporteResponseDto?> GetReporteAsync(DateTimeOffset? desde, DateTimeOffset? hasta);
}
public class ReportesService (HttpClient http) : IReportesService
{
    public async Task<ReporteResponseDto?> GetReporteAsync(DateTimeOffset? desde, DateTimeOffset? hasta)
    {
        try
        {
            
            //Si ambos son null, fallback de api de ultimos 12 meses, devuelvo la uri armada
            string resultado = (desde, hasta) switch
            {
                (null, null) => "",
                (not null, null) => $"?desde={Uri.EscapeDataString(desde.Value.ToString("O"))}",
                (null, not null) => $"?hasta={Uri.EscapeDataString(hasta.Value.ToString("O"))}",
                (not null, not null) => $"?desde={Uri.EscapeDataString(desde.Value.ToString("O"))}" +
                                        $"&hasta={Uri.EscapeDataString(hasta.Value.ToString("O"))}"

            };
            
            return await http
                    .GetFromJsonAsync<ReporteResponseDto?>
                        ($"tickets/reportes{resultado}");
            
        }
        catch
        {
            return null;
        }
    }
}
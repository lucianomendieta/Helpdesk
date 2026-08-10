using System.Diagnostics;
using System.Net.Http.Json;

using Helpdesk.Web.Dtos;

namespace Helpdesk.Web.Services;

public interface IReportesService
{
    Task<ReporteResponseDto?> GetReporteAsync(DateTimeOffset? desde, DateTimeOffset? hasta);
    Task<Stream?> GetReportePdfAsync(DateTimeOffset? desde, DateTimeOffset? hasta);
}
public class ReportesService (HttpClient http) : IReportesService
{
    public async Task<ReporteResponseDto?> GetReporteAsync(DateTimeOffset? desde, DateTimeOffset? hasta)
    {
        try
        {
            
            //Si ambos son null, fallback de api de ultimos 12 meses, devuelvo la uri armada
            string query = ArmarQuery(desde, hasta);
            
            return await http
                    .GetFromJsonAsync<ReporteResponseDto?>
                        ($"tickets/reportes{query}");
            
        }
        catch
        {
            return null;
        }
    }
    
    //Exporta el pdf
    public async Task<Stream?> GetReportePdfAsync(DateTimeOffset? desde, DateTimeOffset? hasta)
    {
        try
        {
            //Si ambos son null, fallback de api de ultimos 12 meses, devuelvo la uri armada
            string query = ArmarQuery(desde, hasta);

            //Espero la respuesta del getcontenido, si fue exitosa: leo el stream, sino: null
            var response = await http.GetAsync($"tickets/reportes/pdf{query}");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStreamAsync() : null;
        }
        catch
        {
            return null;
        }
    }
    
    
    
    
    //Armar el query para evitar repetir
    private static string ArmarQuery(DateTimeOffset? desde, DateTimeOffset? hasta)
    {
        string resultado = (desde, hasta) switch
        {
            (null, null) => "",
            (not null, null) => $"?desde={Uri.EscapeDataString(desde.Value.ToString("O"))}",
            (null, not null) => $"?hasta={Uri.EscapeDataString(hasta.Value.ToString("O"))}",
            (not null, not null) => $"?desde={Uri.EscapeDataString(desde.Value.ToString("O"))}" +
                                    $"&hasta={Uri.EscapeDataString(hasta.Value.ToString("O"))}"

        };

        return resultado;
    }
}
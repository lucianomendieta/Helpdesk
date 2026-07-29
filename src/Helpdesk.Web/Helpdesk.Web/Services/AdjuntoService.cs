using System.Net.Http.Headers;
using System.Net.Http.Json;

using Helpdesk.Web.Dtos;
using Helpdesk.Web.Json;

using Microsoft.AspNetCore.Components.Forms;

namespace Helpdesk.Web.Services;

public interface IAdjuntoService
{
    Task<AdjuntoResponseDto[]?> ListarAdjuntosAsync(int ticketId);
    Task<AdjuntoResponseDto?> SubirAdjuntoAsync(int ticketId, IBrowserFile archivo );
    Task<Stream?> MostrarAdjuntoAsync(int ticketId, int adjuntoId);
}
public class AdjuntoService (HttpClient http) : IAdjuntoService
{
    //Limites constantes
    private const long MaxSize = 10 * 1024 * 1024; //10MB


    //Consulto al endpoint, si no se puede tiro null
    public async Task<AdjuntoResponseDto[]?> ListarAdjuntosAsync(int ticketId)
    {
        try
        {
            return await http.GetFromJsonAsync<AdjuntoResponseDto[]?>($"tickets/{ticketId}/adjuntos", JsonConfig.Options) ;
        }
        catch
        {
            return null;
        }
    }

    //Subo el archivo
    public async Task<AdjuntoResponseDto?> SubirAdjuntoAsync(int ticketId, IBrowserFile archivo)
    {
        try
        {//Instancio el container
            using MultipartFormDataContent multipart = new();
            //armo el stream y seteo el maximo de megas a 10
            var contenido = new StreamContent(archivo.OpenReadStream(MaxSize));
            //seteo el tipo de contenido al que mande mi archivo
            contenido.Headers.ContentType = new MediaTypeHeaderValue(archivo.ContentType);

            //agrego todo al container para enviar el request por la api
            multipart.Add(contenido, "archivo", archivo.Name);

            //Si la respuesta es exitosa, devuelvo el json de exito con el contenido, si no, null
            var response = await http.PostAsync($"tickets/{ticketId}/adjuntos", multipart);
            return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<AdjuntoResponseDto>(JsonConfig.Options) : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    //Descargo el contenido del archivo
    public async Task<Stream?> MostrarAdjuntoAsync(int ticketId, int adjuntoId)
    {
        try 
        {//Espero la respuesta del getcontenido, si fue exitosa: leo el stream, sino: null
        var response = await http.GetAsync($"tickets/{ticketId}/adjuntos/{adjuntoId}/contenido");
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStreamAsync() : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }
}

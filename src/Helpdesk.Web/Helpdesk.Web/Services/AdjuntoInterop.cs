using Microsoft.JSInterop;

namespace Helpdesk.Web.Services;

public class AdjuntoInterop(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _objReference = null;
    
    //Si la referencia sigue vacia, se asigna con lo que tengamos en el archivo de la ruta puesta
    private async Task<IJSObjectReference> FillObjRef()
    {
        _objReference ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/adjuntos.js");
        return _objReference;
    }

    public async Task<string> CreateUrl(Stream stream, string contentType)
    {
        //instancio el modulo de referencia
        using DotNetStreamReference streamReference = new(stream);
        //importo la funcion de mi archivo js pasandole los parametros
        var modulo = await FillObjRef();
        return await modulo.InvokeAsync<string>("createURL", streamReference, contentType);
    }

    //Dispongo de la url, mismo caso anterior, saco de la funcion importada.
    public async Task RevokeUrl(string url)
    {
        var modulo = await FillObjRef();
        await modulo.InvokeVoidAsync("revokeURL", url);
    }

    //Si el obj reference no es null, se elimina
    public async ValueTask DisposeAsync()
    {
        if (_objReference != null) { await _objReference.DisposeAsync(); }
    }

    //Creo la descarga de los arcivos
    public async Task DescargarArchivo(Stream stream, string contentType, string nombreArchivo)
    {
        //instancio el modulo de referencia
        using DotNetStreamReference streamReference = new(stream);
        //importo la funcion de mi archivo js pasandole los parametros
        var modulo = await FillObjRef();
        await modulo.InvokeVoidAsync("documentURL", streamReference, contentType, nombreArchivo);
    }
}

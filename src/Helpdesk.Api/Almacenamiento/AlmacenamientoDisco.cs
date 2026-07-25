

namespace Helpdesk.Api.Almacenamiento;


public interface IAlmacenamientoAdjuntos
{
    Task GuardarArchivoAsync(Stream entrante, string nombreAlmacenado);
    Task<Stream?> AbrirArchivoAsync(string nombreArchivo);
    Task BorrarArchivoAsync(string nombreArchivo);
}

public class AlmacenamientoDisco: IAlmacenamientoAdjuntos
{
    private readonly string _path;

    public AlmacenamientoDisco(IConfiguration configuration)
    {
        _path = configuration["Almacenamiento:RutaAdjuntos"] ?? throw new InvalidOperationException("Falta definir el path de los adjuntos.");
        Directory.CreateDirectory(_path);
    }

    //Metodo para guardar el archivo, recibo el stream y el nombre
    public async Task GuardarArchivoAsync(Stream entrante, string nombreAlmacenado)
    {
        //armo la ruta con el helper
        var ruta = RutaCompleta(nombreAlmacenado);
        //abro el archivo usando la ruta
        await using FileStream destino = new(ruta, FileMode.CreateNew, FileAccess.Write);
        //muevo los bytes a destino
        await entrante.CopyToAsync(destino);
    }

    //Metodo para abrir el archivo, recibo el nombre del archivo
    public Task<Stream?> AbrirArchivoAsync(string nombreArchivo)
    {
        //armo la ruta con el helper
        var ruta = RutaCompleta(nombreArchivo);
        if (!File.Exists(ruta)) //Si no existe el archivo, devuelvo Stream null
        { 
            return Task.FromResult<Stream?>(null); 
        }
        else //Si existe, abro el archivo
        {
            var archivo = File.OpenRead(ruta);
            return Task.FromResult<Stream?>(archivo);
        }
    }

    //Metodo para borrar el archivo, recibe en nombre del mismo
    public Task BorrarArchivoAsync(string nombreArchivo)
    {
        var ruta = RutaCompleta(nombreArchivo);
        //Elimino el archivo
        File.Delete(ruta);
        return Task.CompletedTask;
    }










    //Helper para armar la ruta completa
    private string RutaCompleta(string nombreArchivo)
    {
        return Path.Combine(_path, Path.GetFileName(nombreArchivo)); 
    }
}

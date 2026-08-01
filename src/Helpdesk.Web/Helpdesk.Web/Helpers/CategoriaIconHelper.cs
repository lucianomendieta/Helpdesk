using MudBlazor;

namespace Helpdesk.Web.Helpers;

public static class CategoriaIconHelper
{
    private static readonly Dictionary<string, string> Categoria2Icon = new Dictionary<string, string>
    {
        {"HelpOutline", Icons.Material.Filled.HelpOutline },
        {"Computer", Icons.Material.Filled.Computer },
        {"Key", Icons.Material.Filled.Key },
        {"BeachAccess", Icons.Material.Filled.BeachAccess },
        {"Print", Icons.Material.Filled.Print  },
        {"Wifi", Icons.Material.Filled.Wifi },
        {"Email", Icons.Material.Filled.Email  },
        {"Lock", Icons.Material.Filled.Lock  },
        {"BugReport", Icons.Material.Filled.BugReport },
        {"Handyman", Icons.Material.Filled.Handyman },
        {"Phone", Icons.Material.Filled.Phone },
        {"Badge", Icons.Material.Filled.Badge }
    };

    //Metodo para obtener los valores del diccionario
    public static string ObtenerCategoria2Icon(string clave)
    {
        Categoria2Icon.TryGetValue(clave, out var icon);
        return icon ?? Icons.Material.Filled.HelpOutline;
    }
}

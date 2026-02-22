// SesionUsuario.cs
// ================
// Singleton estatico que almacena los datos del usuario logueado.
// No hereda de MonoBehaviour: es accesible desde cualquier script.

public static class SesionUsuario
{
    public static int    UsuarioId     { get; private set; } = 0;
    public static string NombreUsuario { get; private set; } = "";
    public static bool   HayUsuario    => UsuarioId > 0;

    public static void IniciarSesion(int id, string nombre)
    {
        UsuarioId     = id;
        NombreUsuario = nombre;
    }

    public static void CerrarSesion()
    {
        UsuarioId     = 0;
        NombreUsuario = "";
    }
}

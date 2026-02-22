using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IniciarSesion : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputUsuario;
    public TMP_InputField inputContrasena;
    public Button botonLogin;
    public TextMeshProUGUI mensaje;

    [Header("Base de datos")]
    public string nombreDB = "MyDatabase.sqlite";
    private string rutaDB;

    [Header("Objetos a controlar")]
    public GameObject canvasLogin;
    public GameObject canvasPrincipal;
    public GameObject canvasInventario;

    void Start()
    {
        rutaDB = Application.persistentDataPath + "/" + nombreDB;
        botonLogin.onClick.AddListener(ComprobarLogin);
        mensaje.text = "Introduce usuario y contrasena";
    }

    void ComprobarLogin()
    {
        string usuario = inputUsuario.text.Trim();
        string contrasena = inputContrasena.text.Trim();

        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
        {
            mensaje.text = "Rellena todos los campos";
            return;
        }

        string dbUri = "URI=file:" + rutaDB;

        try
        {
            using (var conexion = new SqliteConnection(dbUri))
            {
                conexion.Open();

                string consulta = "SELECT id, usuario FROM Usuarios WHERE usuario=@usuario AND password=@password";

                using (var comando = new SqliteCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@usuario", usuario);
                    comando.Parameters.AddWithValue("@password", contrasena);

                    using (IDataReader reader = comando.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            string nombre = reader.GetString(1);

                            SesionUsuario.IniciarSesion(id, nombre);

                            mensaje.text = "Inicio de sesion correcto";
                            Debug.Log("Usuario autenticado: " + nombre + " (id=" + id + ")");

                            if (canvasInventario != null) canvasInventario.SetActive(true);
                            if (canvasPrincipal != null) canvasPrincipal.SetActive(false);
                            if (canvasLogin != null) canvasLogin.SetActive(false);
                        }
                        else
                        {
                            mensaje.text = "Usuario o contrasena incorrectos";
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            mensaje.text = "Error al conectar con la base de datos";
            Debug.LogError("Error SQLite: " + e.Message);
        }
    }
}

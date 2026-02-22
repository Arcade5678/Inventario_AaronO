using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RegistrarUsuario : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputUsuario;
    public TMP_InputField inputContraseña;
    public Button botonRegistrar;
    public TextMeshProUGUI mensaje;

    [Header("Base de datos")]
    public string nombreDB = "MyDatabase.sqlite"; 
    private string rutaDB;

    [Header("Objetos a controlar")]
    public GameObject canvasRegistro;    
    public GameObject canvasPrincipal;    

    void Start()
    {
        rutaDB = Application.persistentDataPath + "/" + nombreDB;
        botonRegistrar.onClick.AddListener(RegistrarNuevoUsuario);
        mensaje.text = "Introduce usuario y contraseña";
    }

    void RegistrarNuevoUsuario()
    {
        string usuario = inputUsuario.text.Trim();
        string contraseña = inputContraseña.text.Trim();

        // Validar contraseña
        if (contraseña.Length < 8)
        {
            mensaje.text = "La contraseña debe tener al menos 8 caracteres";
            return;
        }

        if (string.IsNullOrEmpty(usuario))
        {
            mensaje.text = "El usuario no puede estar vacío";
            return;
        }

        string dbUri = "URI=file:" + rutaDB;

        try
        {
            using (var conexion = new SqliteConnection(dbUri))
            {
                conexion.Open();

                string consulta = "INSERT INTO Usuarios (usuario, password) VALUES (@usuario, @password)";

                using (var comando = new SqliteCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@usuario", usuario);
                    comando.Parameters.AddWithValue("@password", contraseña);

                    try
                    {
                        comando.ExecuteNonQuery();
                        mensaje.text = "Usuario registrado correctamente";

                        //limpiar inputs
                        inputUsuario.text = "";
                        inputContraseña.text = "";

                        // Activar canvas principal y desactivar canvas de registro
                        if (canvasPrincipal != null) canvasPrincipal.SetActive(true);
                        if (canvasRegistro != null) canvasRegistro.SetActive(false);
                    }
                    catch (SqliteException e)
                    {
                        if (e.Message.Contains("UNIQUE"))
                        {
                            mensaje.text = "El usuario ya existe";
                        }
                        else
                        {
                            mensaje.text = "Error al registrar usuario";
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

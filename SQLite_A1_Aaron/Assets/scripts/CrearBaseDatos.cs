using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;
using System.IO;

[DefaultExecutionOrder(-100)]
public class CrearBaseDatos : MonoBehaviour
{
    [Header("Configuracion")]
    public bool borrarDatosAlIniciar = false;

    private void Start()
    {
        CreateDatabase();
    }

    private void CreateDatabase()
    {
        string dbPath = Application.persistentDataPath + "/MyDatabase.sqlite";

        if (borrarDatosAlIniciar && File.Exists(dbPath))
        {
            File.Delete(dbPath);
            Debug.Log("[BD] Base de datos anterior borrada.");
        }

        bool esBDNueva = !File.Exists(dbPath);
        if (esBDNueva)
        {
            File.Create(dbPath).Close();
            Debug.Log("[BD] Fichero de base de datos creado en: " + dbPath);
        }

        string dbUri = "URI=file:" + dbPath;

        using (IDbConnection dbConnection = new SqliteConnection(dbUri))
        {
            dbConnection.Open();

            using (IDbCommand cmd = dbConnection.CreateCommand())
            {
                cmd.CommandText =
                    "CREATE TABLE IF NOT EXISTS Usuarios (" +
                    "id       INTEGER PRIMARY KEY AUTOINCREMENT, " +
                    "usuario  TEXT    NOT NULL UNIQUE, " +
                    "password TEXT    NOT NULL CHECK (length(password) >= 8)" +
                    ");";
                cmd.ExecuteNonQuery();
            }

            using (IDbCommand cmd = dbConnection.CreateCommand())
            {
                cmd.CommandText =
                    "INSERT OR IGNORE INTO Usuarios(usuario, password) " +
                    "VALUES('admin', 'admin1234');";
                cmd.ExecuteNonQuery();
            }

            Debug.Log("[BD] Tabla Usuarios lista. Usuario 'admin' / 'admin1234' disponible.");
        }

        InventarioDAL.InicializarTablas();

        Debug.Log("[BD] Base de datos inicializada correctamente.");
    }
}

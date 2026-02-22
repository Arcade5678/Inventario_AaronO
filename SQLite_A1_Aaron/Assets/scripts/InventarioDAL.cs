using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;


[Serializable]
public class ObjetoRPG
{
    public int    Id;
    public string Nombre;
    public string Descripcion;
    public string Tipo;
    public bool   Acumulable;
}

[Serializable]
public class EntradaInventario
{
    public int    Id;
    public int    UsuarioId;
    public int    ObjetoId;
    public string NombreObjeto;
    public string Descripcion;
    public string Tipo;
    public bool   Acumulable;
    public int    Cantidad;
}

[Serializable]
public class RegistroUso
{
    public int      Id;
    public int      InventarioId;
    public string   NombreObjeto;
    public DateTime FechaUso;
    public string   Nota;
}

public static class InventarioDAL
{
    private static string DbUri =>
        "URI=file:" + Application.persistentDataPath + "/MyDatabase.sqlite";

    public static void InicializarTablas()
    {
        try
        {
            using (var conexion = new SqliteConnection(DbUri))
            {
                conexion.Open();

                EjecutarSQL(conexion,
                    "CREATE TABLE IF NOT EXISTS Objetos (" +
                    "id          INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "nombre      TEXT    NOT NULL UNIQUE," +
                    "descripcion TEXT    NOT NULL DEFAULT ''," +
                    "tipo        TEXT    NOT NULL DEFAULT 'General'," +
                    "acumulable  INTEGER NOT NULL DEFAULT 1" +
                    ");");

                EjecutarSQL(conexion,
                    "CREATE TABLE IF NOT EXISTS Inventario (" +
                    "id         INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "usuario_id INTEGER NOT NULL," +
                    "objeto_id  INTEGER NOT NULL," +
                    "cantidad   INTEGER NOT NULL DEFAULT 1 CHECK(cantidad >= 0)," +
                    "FOREIGN KEY (usuario_id) REFERENCES Usuarios(id) ON DELETE CASCADE," +
                    "FOREIGN KEY (objeto_id)  REFERENCES Objetos(id)  ON DELETE CASCADE," +
                    "UNIQUE(usuario_id, objeto_id)" +
                    ");");

                EjecutarSQL(conexion,
                    "CREATE TABLE IF NOT EXISTS HistorialUso (" +
                    "id            INTEGER PRIMARY KEY AUTOINCREMENT," +
                    "inventario_id INTEGER NOT NULL," +
                    "fecha_uso     TEXT    NOT NULL," +
                    "nota          TEXT    DEFAULT ''," +
                    "FOREIGN KEY (inventario_id) REFERENCES Inventario(id) ON DELETE CASCADE" +
                    ");");

                InsertarObjetosEjemplo(conexion);

                Debug.Log("[InventarioDAL] Tablas de inventario inicializadas.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[InventarioDAL] Error al inicializar tablas: " + ex.Message);
        }
    }

    private static void InsertarObjetosEjemplo(IDbConnection conexion)
    {
        using (var check = conexion.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM Objetos;";
            long count = (long)check.ExecuteScalar();
            if (count > 0) return;
        }

        string[] inserts = {
            "INSERT INTO Objetos(nombre,descripcion,tipo,acumulable) VALUES('Espada de Hierro','Espada basica de hierro forjado','Arma',0);",
            "INSERT INTO Objetos(nombre,descripcion,tipo,acumulable) VALUES('Escudo de Madera','Escudo sencillo de madera','Armadura',0);",
            "INSERT INTO Objetos(nombre,descripcion,tipo,acumulable) VALUES('Pocion de Salud','Restaura 50 puntos de vida','Pocion',1);",
            "INSERT INTO Objetos(nombre,descripcion,tipo,acumulable) VALUES('Pocion de Mana','Restaura 30 puntos de mana','Pocion',1);",
            "INSERT INTO Objetos(nombre,descripcion,tipo,acumulable) VALUES('Madera','Material basico de construccion','Material',1);",
            "INSERT INTO Objetos(nombre,descripcion,tipo,acumulable) VALUES('Piedra Magica','Amplifica los hechizos','Especial',1);",
            "INSERT INTO Objetos(nombre,descripcion,tipo,acumulable) VALUES('Amuleto de Fortuna','Aumenta el oro obtenido','Accesorio',0);",
            "INSERT INTO Objetos(nombre,descripcion,tipo,acumulable) VALUES('Flechas','Municion para arco','Municion',1);",
        };

        foreach (var sql in inserts)
            EjecutarSQL(conexion, sql);

        Debug.Log("[InventarioDAL] Objetos de ejemplo insertados.");
    }

    public static List<EntradaInventario> ObtenerInventario(int usuarioId)
    {
        var lista = new List<EntradaInventario>();
        try
        {
            using (var conexion = new SqliteConnection(DbUri))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT i.id, i.usuario_id, i.objeto_id, i.cantidad," +
                        " o.nombre, o.descripcion, o.tipo, o.acumulable" +
                        " FROM Inventario i" +
                        " JOIN Objetos o ON o.id = i.objeto_id" +
                        " WHERE i.usuario_id = @uid" +
                        " ORDER BY o.tipo, o.nombre;";
                    cmd.Parameters.Add(new SqliteParameter("@uid", usuarioId));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new EntradaInventario
                            {
                                Id           = reader.GetInt32(0),
                                UsuarioId    = reader.GetInt32(1),
                                ObjetoId     = reader.GetInt32(2),
                                Cantidad     = reader.GetInt32(3),
                                NombreObjeto = reader.GetString(4),
                                Descripcion  = reader.GetString(5),
                                Tipo         = reader.GetString(6),
                                Acumulable   = reader.GetInt32(7) == 1,
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[InventarioDAL] ObtenerInventario: " + ex.Message);
        }
        return lista;
    }

    public static bool AnadirObjeto(int usuarioId, int objetoId, int cantidad = 1)
    {
        try
        {
            using (var conexion = new SqliteConnection(DbUri))
            {
                conexion.Open();

                int entradaId = -1;
                int cantidadActual = 0;
                bool existe = false;

                using (var checkCmd = conexion.CreateCommand())
                {
                    checkCmd.CommandText =
                        "SELECT id, cantidad FROM Inventario WHERE usuario_id=@uid AND objeto_id=@oid;";
                    checkCmd.Parameters.Add(new SqliteParameter("@uid", usuarioId));
                    checkCmd.Parameters.Add(new SqliteParameter("@oid", objetoId));
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            existe        = true;
                            entradaId     = reader.GetInt32(0);
                            cantidadActual = reader.GetInt32(1);
                        }
                    }
                }

                if (existe)
                {
                    bool acumulable = EsAcumulable(conexion, objetoId);
                    if (!acumulable)
                    {
                        Debug.LogWarning("[InventarioDAL] El objeto no es acumulable y ya esta en el inventario.");
                        return false;
                    }
                    return ActualizarCantidad(usuarioId, objetoId, cantidadActual + cantidad);
                }
                else
                {
                    using (var insCmd = conexion.CreateCommand())
                    {
                        insCmd.CommandText =
                            "INSERT INTO Inventario(usuario_id, objeto_id, cantidad) VALUES(@uid,@oid,@qty);";
                        insCmd.Parameters.Add(new SqliteParameter("@uid", usuarioId));
                        insCmd.Parameters.Add(new SqliteParameter("@oid", objetoId));
                        insCmd.Parameters.Add(new SqliteParameter("@qty", cantidad));
                        insCmd.ExecuteNonQuery();
                    }
                    Debug.Log("[InventarioDAL] Objeto " + objetoId + " anadido al inventario del usuario " + usuarioId);
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[InventarioDAL] AnadirObjeto: " + ex.Message);
            return false;
        }
    }

    public static bool ActualizarCantidad(int usuarioId, int objetoId, int nuevaCantidad)
    {
        try
        {
            using (var conexion = new SqliteConnection(DbUri))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText =
                        "UPDATE Inventario SET cantidad=@qty WHERE usuario_id=@uid AND objeto_id=@oid;";
                    cmd.Parameters.Add(new SqliteParameter("@qty", nuevaCantidad));
                    cmd.Parameters.Add(new SqliteParameter("@uid", usuarioId));
                    cmd.Parameters.Add(new SqliteParameter("@oid", objetoId));
                    int rows = cmd.ExecuteNonQuery();
                    Debug.Log("[InventarioDAL] Cantidad actualizada (" + rows + " filas).");
                    return rows > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[InventarioDAL] ActualizarCantidad: " + ex.Message);
            return false;
        }
    }

    public static bool EliminarObjeto(int usuarioId, int objetoId)
    {
        try
        {
            using (var conexion = new SqliteConnection(DbUri))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText =
                        "DELETE FROM Inventario WHERE usuario_id=@uid AND objeto_id=@oid;";
                    cmd.Parameters.Add(new SqliteParameter("@uid", usuarioId));
                    cmd.Parameters.Add(new SqliteParameter("@oid", objetoId));
                    int rows = cmd.ExecuteNonQuery();
                    Debug.Log("[InventarioDAL] Objeto eliminado (" + rows + " filas).");
                    return rows > 0;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[InventarioDAL] EliminarObjeto: " + ex.Message);
            return false;
        }
    }

    public static List<ObjetoRPG> ObtenerCatalogoObjetos()
    {
        var lista = new List<ObjetoRPG>();
        try
        {
            using (var conexion = new SqliteConnection(DbUri))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, nombre, descripcion, tipo, acumulable FROM Objetos ORDER BY tipo, nombre;";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ObjetoRPG
                            {
                                Id          = reader.GetInt32(0),
                                Nombre      = reader.GetString(1),
                                Descripcion = reader.GetString(2),
                                Tipo        = reader.GetString(3),
                                Acumulable  = reader.GetInt32(4) == 1,
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[InventarioDAL] ObtenerCatalogoObjetos: " + ex.Message);
        }
        return lista;
    }

    public static bool RegistrarUso(int inventarioId, string nota = "")
    {
        try
        {
            using (var conexion = new SqliteConnection(DbUri))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText =
                        "INSERT INTO HistorialUso(inventario_id, fecha_uso, nota) VALUES(@iid, @fecha, @nota);";
                    cmd.Parameters.Add(new SqliteParameter("@iid",   inventarioId));
                    cmd.Parameters.Add(new SqliteParameter("@fecha", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    cmd.Parameters.Add(new SqliteParameter("@nota",  nota));
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[InventarioDAL] RegistrarUso: " + ex.Message);
            return false;
        }
    }

    public static List<RegistroUso> ObtenerHistorial(int usuarioId)
    {
        var lista = new List<RegistroUso>();
        try
        {
            using (var conexion = new SqliteConnection(DbUri))
            {
                conexion.Open();
                using (var cmd = conexion.CreateCommand())
                {
                    cmd.CommandText =
                        "SELECT h.id, h.inventario_id, o.nombre, h.fecha_uso, h.nota" +
                        " FROM HistorialUso h" +
                        " JOIN Inventario i ON i.id = h.inventario_id" +
                        " JOIN Objetos    o ON o.id = i.objeto_id" +
                        " WHERE i.usuario_id = @uid" +
                        " ORDER BY h.fecha_uso DESC" +
                        " LIMIT 50;";
                    cmd.Parameters.Add(new SqliteParameter("@uid", usuarioId));

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new RegistroUso
                            {
                                Id           = reader.GetInt32(0),
                                InventarioId = reader.GetInt32(1),
                                NombreObjeto = reader.GetString(2),
                                FechaUso     = DateTime.Parse(reader.GetString(3)),
                                Nota         = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[InventarioDAL] ObtenerHistorial: " + ex.Message);
        }
        return lista;
    }

    private static bool EsAcumulable(IDbConnection conexion, int objetoId)
    {
        using (var cmd = conexion.CreateCommand())
        {
            cmd.CommandText = "SELECT acumulable FROM Objetos WHERE id=@oid;";
            cmd.Parameters.Add(new SqliteParameter("@oid", objetoId));
            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) == 1;
        }
    }

    private static void EjecutarSQL(IDbConnection conexion, string sql)
    {
        using (var cmd = conexion.CreateCommand())
        {
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
    }
}
